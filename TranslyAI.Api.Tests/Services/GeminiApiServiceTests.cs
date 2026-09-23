using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using TranslyAI.Api.AppSettings;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Enums;
using TranslyAI.Api.Services;
using TranslyAI.Api.Tests.Fakes;

namespace TranslyAI.Api.Tests.Services;

public class GeminiApiServiceTests
{
    // ── الأدوات ──────────────────────────────────────────────

    private readonly FakeLogger<GeminiApiService> _logger = new();

    private GeminiApiService CreateService(HttpResponseMessage response)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(response));

        var options = Options.Create(new GeminiOptions
        {
            ApiKey = "test-key",
            Model = "test-model",
        });

        return new GeminiApiService(_logger, httpClient, options);
    }

    private static HttpResponseMessage Respond(
        HttpStatusCode status,
        string body,
        string mediaType = "application/json")
        => new(status) { Content = new StringContent(body, Encoding.UTF8, mediaType) };

    private static TranslationRequestDto AnyRequest() => new()
    {
        Text = "Hello",
        SourceLanguage = "en",
        TargetLanguage = "ar",
    };

    // ── 1. الطريق الناجح ─────────────────────────────────────

    [Fact]
    public async Task TranslateAsync_WhenInteractionCompletes_ReturnsSuccessWithTextAndModel()
    {
        const string body = """
        {
          "id": "int_123",
          "status": "completed",
          "model": "gemini-3.7-flash",
          "steps": [
            {
              "type": "model_output",
              "status": "completed",
              "content": [ { "type": "text", "text": "مرحبا" } ]
            }
          ]
        }
        """;

        var service = CreateService(Respond(HttpStatusCode.OK, body));

        var outcome = await service.TranslateAsync(AnyRequest(), CancellationToken.None);

        var success = Assert.IsType<TranslationOutcome.Success>(outcome);
        Assert.Equal("مرحبا", success.Text);
        Assert.Equal("gemini-3.7-flash", success.Model);
    }

    // ── 2. الترتيب مش مهم — الاستخراج بالنوع ────────────────

    [Fact]
    public async Task TranslateAsync_WhenModelOutputIsNotTheFirstStep_StillExtractsItsText()
    {
        const string body = """
        {
          "status": "completed",
          "model": "gemini-3.7-flash",
          "steps": [
            {
              "type": "reasoning",
              "content": [ { "type": "text", "text": "INTERNAL THOUGHTS" } ]
            },
            {
              "type": "model_output",
              "content": [ { "type": "text", "text": "مرحبا" } ]
            }
          ]
        }
        """;

        var service = CreateService(Respond(HttpStatusCode.OK, body));

        var outcome = await service.TranslateAsync(AnyRequest(), CancellationToken.None);

        var success = Assert.IsType<TranslationOutcome.Success>(outcome);
        Assert.Equal("مرحبا", success.Text);
        Assert.DoesNotContain("INTERNAL", success.Text);
    }

    // ── 3. 429 + Retry-After ─────────────────────────────────

    [Fact]
    public async Task TranslateAsync_WhenGeminiReturns429_ReturnsRateLimitedWithRetryAfterFromHeader()
    {
        var response = Respond(HttpStatusCode.TooManyRequests, """{ "error": "quota" }""");
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));

        var service = CreateService(response);

        var outcome = await service.TranslateAsync(AnyRequest(), CancellationToken.None);

        var rateLimited = Assert.IsType<TranslationOutcome.RateLimited>(outcome);
        Assert.Equal(TimeSpan.FromSeconds(30), rateLimited.RetryAfter);
    }

    // ── 4. JSON بايظ ─────────────────────────────────────────

    [Fact]
    public async Task TranslateAsync_WhenBodyIsTruncatedJson_ReturnsUpstreamError()
    {
        // 200 + Content-Type صح + JSON مقصوص في النص
        const string body = """{ "status": "completed", """;

        var service = CreateService(Respond(HttpStatusCode.OK, body));

        var outcome = await service.TranslateAsync(AnyRequest(), CancellationToken.None);

        Assert.IsType<TranslationOutcome.UpstreamError>(outcome);
    }

    [Fact]
    public async Task TranslateAsync_When200BodyIsNotJsonAtAll_ReturnsUpstreamError()
    {
        // 200 بس الـ Content-Type نفسه مش JSON — تخيّل proxy رجّع صفحة HTML
        var service = CreateService(
            Respond(HttpStatusCode.OK, "<html>502 Bad Gateway</html>", mediaType: "text/html"));

        var outcome = await service.TranslateAsync(AnyRequest(), CancellationToken.None);

        Assert.IsType<TranslationOutcome.UpstreamError>(outcome);
    }

    // ── 5. status مش معروف → التحذير لازم يتسجّل ─────────────

    [Fact]
    public async Task TranslateAsync_WhenStatusIsUnrecognised_LogsAWarning()
    {
        const string body = """
        {
          "status": "bogus",
          "model": "gemini-3.7-flash",
          "steps": []
        }
        """;

        var service = CreateService(Respond(HttpStatusCode.OK, body));

        var outcome = await service.TranslateAsync(AnyRequest(), CancellationToken.None);

        // الـ assert الأساسي: التحذير اتنفّذ فعلاً
        var warning = Assert.Single(
            _logger.Collector.GetSnapshot(),
            record => record.Level == LogLevel.Warning);

        Assert.Contains("bogus", warning.Message);

        // ثانوي: القيمة الراجعة. لاحظ إنها كانت بتعدّي صح حتى والتحذير بايظ.
        var notCompleted = Assert.IsType<TranslationOutcome.NotCompleted>(outcome);
        Assert.Equal(GeminiInteractionStatus.Unknown, notCompleted.Status);
    }
}
