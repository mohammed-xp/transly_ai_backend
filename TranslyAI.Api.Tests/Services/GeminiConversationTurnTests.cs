using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TranslyAI.Api.AppSettings;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Services;
using TranslyAI.Api.Services.Result;

namespace TranslyAI.Api.Tests.Services;

public class GeminiConversationTurnTests
{
    private const string ConversationModel = "gemini-conversation-test";
    private static readonly byte[] AnyWav = [1, 2, 3, 4];

    // ── 1. ردود Gemini المفهومة ──────────────────────────────

    [Fact]
    public async Task WhenStatusIsOk_ReturnsTranslatedWithSourceAndTarget()
    {
        var (service, _) = CreateService(CompletedWith(new
        {
            status = "ok",
            source_language = "AR",
            transcript = "  إزيك؟  ",
            translation = " How are you? ",
        }));

        var outcome = await TranslateTurnAsync(service);

        var translated = Assert.IsType<ConversationOutcome.Translated>(outcome);
        Assert.Equal("ar", translated.SourceLanguage);
        Assert.Equal("en", translated.TargetLanguage);
        Assert.Equal("إزيك؟", translated.Transcript);
        Assert.Equal("How are you?", translated.Translation);
        Assert.Equal("gemini-test-audio", translated.Model);
    }

    [Fact]
    public async Task WhenStatusIsNoSpeech_ReturnsNoSpeech()
    {
        var (service, _) = CreateService(CompletedWith(new { status = "no_speech" }));

        var outcome = await TranslateTurnAsync(service);

        Assert.IsType<ConversationOutcome.NoSpeech>(outcome);
    }

    [Fact]
    public async Task WhenStatusIsOkButTranscriptIsEmpty_ReturnsNoSpeech()
    {
        var (service, _) = CreateService(CompletedWith(new
        {
            status = "ok",
            source_language = "ar",
            transcript = "   ",
            translation = "",
        }));

        var outcome = await TranslateTurnAsync(service);

        Assert.IsType<ConversationOutcome.NoSpeech>(outcome);
    }

    [Fact]
    public async Task WhenStatusIsUnsupportedLanguage_ReturnsUnsupportedLanguage()
    {
        var (service, _) = CreateService(CompletedWith(new { status = "unsupported_language" }));

        var outcome = await TranslateTurnAsync(service);

        Assert.IsType<ConversationOutcome.UnsupportedLanguage>(outcome);
    }

    // ── 2. رد مش منطقي → 502 ─────────────────────────────────

    [Fact]
    public async Task WhenSourceLanguageIsOutsideThePair_ReturnsUpstreamError()
    {
        var (service, _) = CreateService(CompletedWith(new
        {
            status = "ok",
            source_language = "fr",
            transcript = "Bonjour",
            translation = "مرحبا",
        }));

        var outcome = await TranslateTurnAsync(service);

        AssertUpstreamError(outcome);
    }

    [Fact]
    public async Task WhenModelTextIsNotJson_ReturnsUpstreamError()
    {
        var (service, _) = CreateService(CompletedWithText("this is not json"));

        var outcome = await TranslateTurnAsync(service);

        AssertUpstreamError(outcome);
    }

    // ── 3. أخطاء Gemini بتتلف زي ما هي ───────────────────────

    [Fact]
    public async Task WhenGeminiReturns429_WrapsRateLimitedWithRetryAfter()
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("""{ "error": "quota" }""", Encoding.UTF8, "application/json"),
        };
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));

        var (service, _) = CreateService(response);

        var outcome = await TranslateTurnAsync(service);

        var failure = Assert.IsType<ConversationOutcome.UpstreamFailure>(outcome);
        var rateLimited = Assert.IsType<TranslationOutcome.RateLimited>(failure.Error);
        Assert.Equal(TimeSpan.FromSeconds(30), rateLimited.RetryAfter);
    }

    // ── 4. شكل الـ request اللي رايح لـ Gemini ───────────────

    [Fact]
    public async Task SendsAudioHistoryAndSchemaInTheInteractionsShape()
    {
        var (service, handler) = CreateService(CompletedWith(new { status = "no_speech" }));

        await TranslateTurnAsync(service,
        [
            new ConversationHistoryItemDto { Language = "ar", Text = "إزيك؟" },
        ]);

        var body = JsonNode.Parse(handler.RequestBody!)!;

        Assert.Equal(ConversationModel, body["model"]!.GetValue<string>());
        Assert.False(body["store"]!.GetValue<bool>());
        Assert.Contains("Arabic", body["system_instruction"]!.GetValue<string>());

        var input = body["input"]!.AsArray();
        Assert.Equal(2, input.Count);

        // الـ history: نص، والعربي مكتوب زي ما هو (مش \u0625...)
        Assert.Equal("text", input[0]!["type"]!.GetValue<string>());
        Assert.Contains("إزيك؟", input[0]!["text"]!.GetValue<string>());

        // الصوت: آخر جزء، inline بـ base64
        Assert.Equal("audio", input[1]!["type"]!.GetValue<string>());
        Assert.Equal("audio/wav", input[1]!["mime_type"]!.GetValue<string>());
        Assert.Equal(Convert.ToBase64String(AnyWav), input[1]!["data"]!.GetValue<string>());

        // الـ enum بتاع اللغة = اللغتين اللي المستخدم اختارهم بس
        var languages = body["response_format"]!["schema"]!["properties"]!["source_language"]!["enum"]!
            .AsArray()
            .Select(node => node!.GetValue<string>());
        Assert.Equal(new[] { "ar", "en" }, languages);
    }

    [Fact]
    public async Task WhenConversationModelIsEmpty_FallsBackToModel()
    {
        var (service, handler) = CreateService(CompletedWith(new { status = "no_speech" }), conversationModel: "");

        await TranslateTurnAsync(service);

        var body = JsonNode.Parse(handler.RequestBody!)!;
        Assert.Equal("test-model", body["model"]!.GetValue<string>());
    }

    // ── الأدوات ──────────────────────────────────────────────

    private static Task<ConversationOutcome> TranslateTurnAsync(
        GeminiApiService service,
        IReadOnlyList<ConversationHistoryItemDto>? history = null) =>
        service.TranslateConversationTurnAsync(AnyWav, "ar", "en", history ?? [], CancellationToken.None);

    private static (GeminiApiService Service, CapturingHandler Handler) CreateService(
        HttpResponseMessage response,
        string? conversationModel = ConversationModel)
    {
        var handler = new CapturingHandler(response);

        var options = Options.Create(new GeminiOptions
        {
            ApiKey = "test-key",
            Model = "test-model",
            ConversationModel = conversationModel,
        });

        var service = new GeminiApiService(NullLogger<GeminiApiService>.Instance, new HttpClient(handler), options);
        return (service, handler);
    }

    private static HttpResponseMessage CompletedWith(object payload) =>
        CompletedWithText(JsonSerializer.Serialize(payload));

    private static HttpResponseMessage CompletedWithText(string modelText)
    {
        var envelope = new
        {
            status = "completed",
            model = "gemini-test-audio",
            steps = new[]
            {
                new
                {
                    type = "model_output",
                    content = new[] { new { type = "text", text = modelText } },
                },
            },
        };

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(envelope), Encoding.UTF8, "application/json"),
        };
    }

    private static void AssertUpstreamError(ConversationOutcome outcome)
    {
        var failure = Assert.IsType<ConversationOutcome.UpstreamFailure>(outcome);
        Assert.IsType<TranslationOutcome.UpstreamError>(failure.Error);
    }

    // بيرجّع رد ثابت، وبيحفظ الـ body اللي اتبعت عشان نفحصه
    private sealed class CapturingHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return response;
        }
    }
}