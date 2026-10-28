using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TranslyAI.Api.AppSettings;
using TranslyAI.Api.Data;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Entities;
using TranslyAI.Api.Enums;
using TranslyAI.Api.Tests.Fakes;

namespace TranslyAI.Api.Tests.Integration;

public class ConversationsEndpointTests(TranslyApiFactory factory)
    : IClassFixture<TranslyApiFactory>, IAsyncLifetime
{
    private const string Password = "Passw0rd!123";
    private const string TurnsUrl = "/api/v1/conversations/turns";

    public async Task InitializeAsync()
    {
        factory.Gemini.Reset();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TranslyDbContext>();

        await db.ConversationUsages.ExecuteDeleteAsync();
        await db.Users.ExecuteDeleteAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── 1. الـ endpoint مقفول ────────────────────────────────

    [Fact]
    public async Task PostTurn_WithoutToken_IsRejected()
    {
        var client = factory.CreateClient();

        var response = await PostTurnAsync(client, accessToken: null, TestWav.Create(seconds: 2));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, factory.Gemini.CallCount);
    }

    // ── 2. الطريق الناجح + الـ history بـ format الـ Flutter ──

    [Fact]
    public async Task PostTurn_WhenGeminiTranslates_ReturnsTheTurnAndAudioQuotaHeaders()
    {
        factory.Gemini.RespondWith = () => GeminiTurn(new
        {
            status = "ok",
            source_language = "ar",
            transcript = "عامل إيه؟",
            translation = "How are you doing?",
        });

        var client = factory.CreateClient();
        var token = await RegisterAndLoginAsync(client, "turn-ok@transly.test");
        var limits = Limits();

        // الـ history بنفس الـ format اللي Flutter هيبعته: history[0].language / history[0].text
        var response = await PostTurnAsync(client, token, TestWav.Create(seconds: 2),
            history: [("en", "Hi there"), ("ar", "أهلاً")]);

        var turn = await ApiResponseAssert.SuccessAsync<ConversationTurnResponseDto>(response);
        Assert.Equal(ConversationTurnStatus.Ok, turn.Status);
        Assert.Equal("عامل إيه؟", turn.Transcript);
        Assert.Equal("How are you doing?", turn.Translation);
        Assert.Equal("ar", turn.SourceLanguage!.Code);
        Assert.Equal("en", turn.TargetLanguage!.Code);
        Assert.Equal(2, turn.AudioSeconds);

        Assert.Equal(Invariant(limits.AudioSecondsPerWindow), Header(response, "X-Audio-Seconds-Limit"));
        Assert.Equal(Invariant(limits.AudioSecondsPerWindow - 2), Header(response, "X-Audio-Seconds-Remaining"));
    }

    // ── 3. مفيش كلام = 200 ومن غير خصم ───────────────────────

    [Fact]
    public async Task PostTurn_WhenGeminiHearsNoSpeech_Returns200WithNoSpeechStatus()
    {
        factory.Gemini.RespondWith = () => GeminiTurn(new { status = "no_speech" });

        var client = factory.CreateClient();
        var token = await RegisterAndLoginAsync(client, "no-speech@transly.test");
        var limits = Limits();

        var response = await PostTurnAsync(client, token, TestWav.Create(seconds: 3));

        var turn = await ApiResponseAssert.SuccessAsync<ConversationTurnResponseDto>(response);
        Assert.Equal(ConversationTurnStatus.NoSpeech, turn.Status);
        Assert.Null(turn.Translation);
        Assert.Equal(Invariant(limits.AudioSecondsPerWindow), Header(response, "X-Audio-Seconds-Remaining"));
    }

    // ── 4. رفض قبل Gemini ───────────────────────────────────

    [Fact]
    public async Task PostTurn_WhenFileIsNotWav_ReturnsValidationProblemOnAudio()
    {
        var client = factory.CreateClient();
        var token = await RegisterAndLoginAsync(client, "not-wav@transly.test");

        var response = await PostTurnAsync(client, token, Encoding.ASCII.GetBytes("ID3 this is an mp3"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        // Assert.Contains("audio", problem.Errors.Keys);
        Assert.Contains(nameof(ConversationTurnRequestDto.Audio), problem.Errors.Keys);
        Assert.Equal(0, factory.Gemini.CallCount);
    }

    [Fact]
    public async Task PostTurn_WithSameLanguages_IsRejectedBeforeCallingGemini()
    {
        var client = factory.CreateClient();
        var token = await RegisterAndLoginAsync(client, "same-lang@transly.test");

        var response = await PostTurnAsync(client, token, TestWav.Create(seconds: 2), languageA: "ar", languageB: "AR");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, factory.Gemini.CallCount);
    }

    [Fact]
    public async Task PostTurn_WhenAudioQuotaIsExhausted_Returns429WithRetryAfter()
    {
        var client = factory.CreateClient();
        const string email = "audio-quota@transly.test";
        var token = await RegisterAndLoginAsync(client, email);
        await FillAudioQuotaAsync(email);

        var response = await PostTurnAsync(client, token, TestWav.Create(seconds: 2));

        await ApiResponseAssert.ProblemAsync(response, HttpStatusCode.TooManyRequests);
        Assert.Equal("0", Header(response, "X-Audio-Seconds-Remaining"));
        Assert.InRange(response.Headers.RetryAfter!.Delta!.Value, TimeSpan.Zero, TimeSpan.FromDays(1));
        Assert.Equal(0, factory.Gemini.CallCount);
    }

    // ── 5. أخطاء Gemini بنفس الـ mapping بتاع الترجمة ─────────

    [Fact]
    public async Task PostTurn_WhenGeminiIsRateLimited_Returns503()
    {
        factory.Gemini.RespondWith = () => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("""{ "error": "quota" }""", Encoding.UTF8, "application/json"),
        };

        var client = factory.CreateClient();
        var token = await RegisterAndLoginAsync(client, "gemini-429@transly.test");

        var response = await PostTurnAsync(client, token, TestWav.Create(seconds: 2));

        await ApiResponseAssert.ProblemAsync(response, HttpStatusCode.ServiceUnavailable);
    }

    // ── الأدوات ──────────────────────────────────────────────

    private static async Task<HttpResponseMessage> PostTurnAsync(
        HttpClient client,
        string? accessToken,
        byte[] audio,
        string languageA = "ar",
        string languageB = "en",
        IReadOnlyList<(string Language, string Text)>? history = null)
    {
        var form = new MultipartFormDataContent();

        var audioContent = new ByteArrayContent(audio);
        audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        form.Add(audioContent, "audio", "turn.wav");

        form.Add(new StringContent(languageA), "languageA");
        form.Add(new StringContent(languageB), "languageB");

        if (history is not null)
        {
            for (var i = 0; i < history.Count; i++)
            {
                form.Add(new StringContent(history[i].Language), $"history[{i}].language");
                form.Add(new StringContent(history[i].Text), $"history[{i}].text");
            }
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, TurnsUrl) { Content = form };

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await client.SendAsync(request);
    }

    private static async Task<string> RegisterAndLoginAsync(HttpClient client, string email)
    {
        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            userName = email.Split('@')[0] + "-user",
            password = Password,
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        var body = await ApiResponseAssert.SuccessAsync<LoginResponseDto>(login);

        return body.Token.AccessToken;
    }

    private async Task FillAudioQuotaAsync(string email)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TranslyDbContext>();

        var userId = await db.Users
            .Where(user => user.Email == email)
            .Select(user => user.Id)
            .SingleAsync();

        db.ConversationUsages.Add(new ConversationUsage
        {
            UserId = userId,
            LanguageA = "ar",
            LanguageB = "en",
            AudioSeconds = Limits().AudioSecondsPerWindow,
            Status = UsageStatus.Committed,
            CreatedAtUtc = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();
    }

    private PlanLimits Limits()
    {
        var plans = factory.Services.GetRequiredService<IOptions<PlansOptions>>().Value;
        return plans.Catalog[plans.DefaultPlan];
    }

    private static HttpResponseMessage GeminiTurn(object payload)
    {
        var envelope = new
        {
            status = "completed",
            model = TranslyApiFactory.TestModel,
            steps = new[]
            {
                new
                {
                    type = "model_output",
                    content = new[] { new { type = "text", text = JsonSerializer.Serialize(payload) } },
                },
            },
        };

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(envelope), Encoding.UTF8, "application/json"),
        };
    }

    private static string Header(HttpResponseMessage response, string name) =>
        response.Headers.GetValues(name).Single();

    private static string Invariant(int value) => value.ToString(CultureInfo.InvariantCulture);
}