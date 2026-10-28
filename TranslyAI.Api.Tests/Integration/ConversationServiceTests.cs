using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TranslyAI.Api.AppSettings;
using TranslyAI.Api.Data;
using TranslyAI.Api.Entities;
using TranslyAI.Api.Enums;
using TranslyAI.Api.Services.IServices;
using TranslyAI.Api.Services.Result;
using TranslyAI.Api.Tests.Fakes;

namespace TranslyAI.Api.Tests.Integration;

public class ConversationServiceTests(TranslyApiFactory factory)
    : IClassFixture<TranslyApiFactory>, IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        factory.Gemini.Reset();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TranslyDbContext>();

        await db.ConversationUsages.ExecuteDeleteAsync();
        await db.Users.ExecuteDeleteAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── 1. الطريق الناجح ─────────────────────────────────────

    [Fact]
    public async Task WhenGeminiTranslates_CommitsTheBilledSeconds()
    {
        factory.Gemini.RespondWith = () => GeminiTurn(new
        {
            status = "ok",
            source_language = "ar",
            transcript = "إزيك؟",
            translation = "How are you?",
        });

        await using var scope = factory.Services.CreateAsyncScope();
        var userId = await CreateUserAsync(scope);

        // 3.05 ثانية → بتتحسب 3 بسبب الـ tolerance
        var result = await TranslateTurnAsync(scope, userId, TestWav.Create(seconds: 3.05));

        var translated = Assert.IsType<ConversationOutcome.Translated>(result.Outcome);
        Assert.Equal("How are you?", translated.Translation);
        Assert.Equal(3, result.AudioSeconds);
        Assert.Equal(3, result.Quota.Used);

        var usage = Assert.Single(await UsagesAsync(scope, userId));
        Assert.Equal(UsageStatus.Committed, usage.Status);
        Assert.Equal(3, usage.AudioSeconds);
        Assert.Equal(1, factory.Gemini.CallCount);
    }

    // ── 2. Gemini ردّ بس مفيش ترجمة → الحجز بيرجع ───────────

    [Fact]
    public async Task WhenGeminiHearsNoSpeech_ReleasesTheReservation()
    {
        factory.Gemini.RespondWith = () => GeminiTurn(new { status = "no_speech" });

        await using var scope = factory.Services.CreateAsyncScope();
        var userId = await CreateUserAsync(scope);

        var result = await TranslateTurnAsync(scope, userId, TestWav.Create(seconds: 4));

        Assert.IsType<ConversationOutcome.NoSpeech>(result.Outcome);
        Assert.Equal(0, result.Quota.Used);
        Assert.Empty(await UsagesAsync(scope, userId));
        Assert.Equal(1, factory.Gemini.CallCount);
    }

    [Fact]
    public async Task WhenGeminiFails_ReleasesTheReservationAndWrapsTheError()
    {
        factory.Gemini.RespondWith = () => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("""{ "error": "boom" }""", Encoding.UTF8, "application/json"),
        };

        await using var scope = factory.Services.CreateAsyncScope();
        var userId = await CreateUserAsync(scope);

        var result = await TranslateTurnAsync(scope, userId, TestWav.Create(seconds: 2));

        var failure = Assert.IsType<ConversationOutcome.UpstreamFailure>(result.Outcome);
        Assert.IsType<TranslationOutcome.UpstreamError>(failure.Error);
        Assert.Empty(await UsagesAsync(scope, userId));
    }

    // ── 3. رفض قبل ما نكلم Gemini ────────────────────────────

    [Fact]
    public async Task WhenFileIsNotWav_ReturnsInvalidAudioWithoutCallingGemini()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var userId = await CreateUserAsync(scope);

        var result = await TranslateTurnAsync(scope, userId, Encoding.ASCII.GetBytes("ID3 this is an mp3"));

        Assert.IsType<ConversationOutcome.InvalidAudio>(result.Outcome);
        Assert.Equal(0, result.AudioSeconds);
        Assert.Equal(0, factory.Gemini.CallCount);
        Assert.Empty(await UsagesAsync(scope, userId));
    }

    [Fact]
    public async Task WhenRecordingIsTooShort_ReturnsNoSpeechWithoutCallingGemini()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var userId = await CreateUserAsync(scope);

        var result = await TranslateTurnAsync(scope, userId, TestWav.Create(seconds: 0.3));

        Assert.IsType<ConversationOutcome.NoSpeech>(result.Outcome);
        Assert.Equal(0, factory.Gemini.CallCount);
        Assert.Empty(await UsagesAsync(scope, userId));
    }

    [Fact]
    public async Task WhenRecordingIsLongerThanThePlanAllows_ReturnsAudioTooLongWithoutCallingGemini()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var limits = Limits(scope.ServiceProvider);
        var userId = await CreateUserAsync(scope);

        var result = await TranslateTurnAsync(
            scope, userId, TestWav.Create(seconds: limits.MaxAudioSecondsPerRequest + 1));

        var tooLong = Assert.IsType<ConversationOutcome.AudioTooLong>(result.Outcome);
        Assert.Equal(limits.MaxAudioSecondsPerRequest, tooLong.MaxSeconds);
        Assert.Equal(0, factory.Gemini.CallCount);
        Assert.Empty(await UsagesAsync(scope, userId));
    }

    // ── الأدوات ──────────────────────────────────────────────

    private static Task<ConversationResult> TranslateTurnAsync(AsyncServiceScope scope, Guid userId, byte[] audio)
    {
        var service = scope.ServiceProvider.GetRequiredService<IConversationService>();
        return service.TranslateTurnAsync(audio, "ar", "en", [], userId, CancellationToken.None);
    }

    private static Task<List<ConversationUsage>> UsagesAsync(AsyncServiceScope scope, Guid userId) =>
        scope.ServiceProvider.GetRequiredService<TranslyDbContext>()
            .ConversationUsages
            .AsNoTracking()
            .Where(u => u.UserId == userId)
            .ToListAsync();

    private static HttpResponseMessage GeminiTurn(object payload)
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
                    content = new[] { new { type = "text", text = JsonSerializer.Serialize(payload) } },
                },
            },
        };

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(envelope), Encoding.UTF8, "application/json"),
        };
    }

    private static async Task<Guid> CreateUserAsync(AsyncServiceScope scope)
    {
        var db = scope.ServiceProvider.GetRequiredService<TranslyDbContext>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid():N}@transly.test",
            UserName = "conversation-tester",
            CreatedAtUtc = DateTime.UtcNow,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user.Id;
    }

    private static PlanLimits Limits(IServiceProvider services)
    {
        var plans = services.GetRequiredService<IOptions<PlansOptions>>().Value;
        return plans.Catalog[plans.DefaultPlan];
    }
}