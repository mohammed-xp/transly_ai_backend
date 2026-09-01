using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TranslyAI.Api.Data;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Enums;

namespace TranslyAI.Api.Tests.Integration;

public class TranslationsEndpointTests(TranslyApiFactory factory)
    : IClassFixture<TranslyApiFactory>, IAsyncLifetime
{
    private const string Password = "Passw0rd!123";

    // ── تنضيف قبل كل تيست ────────────────────────────────────

    public async Task InitializeAsync()
    {
        factory.Gemini.Reset();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TranslyDbContext>();

        await db.TranslationUsages.ExecuteDeleteAsync();
        await db.CachedTranslations.ExecuteDeleteAsync();
        await db.Users.ExecuteDeleteAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── 1. الـ endpoint مقفول — والـ middleware هي اللي بترفض ──

    [Fact]
    public async Task Translate_WithoutToken_IsRejectedByTheAuthenticationMiddleware()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/v1/translations", AnyTranslation());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate, header => header.Scheme == "Bearer");
    }

    // ── 2. مستخدمين مختلفين / نفس النص ───────────────────────

    [Fact]
    public async Task Translate_WhenTwoUsersSendTheSameText_CallsGeminiOnceAndBillsBoth()
    {
        factory.Gemini.RespondWith = () => GeminiCompleted("مرحبا");

        var client = factory.CreateClient();

        var tokenA = await RegisterAndLoginViaApiAsync(client, "a@transly.test");
        var tokenB = await RegisterAndLoginViaApiAsync(client, "b@transly.test");

        var first = await TranslateAsync(client, tokenA);
        var second = await TranslateAsync(client, tokenB);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        Assert.Equal(1, factory.Gemini.CallCount);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TranslyDbContext>();

        Assert.Equal(1, await db.CachedTranslations.CountAsync());

        var usages = await db.TranslationUsages
            .OrderBy(usage => usage.Id)
            .ToListAsync();

        Assert.Equal(2, usages.Count);
        Assert.Equal(TranslationSource.Gemini, usages[0].Source);
        Assert.Equal(TranslationSource.Cache, usages[1].Source);
        Assert.NotEqual(usages[0].UserId, usages[1].UserId);
    }

    // ── 3. فشل Gemini = صفر فوترة ────────────────────────────

    [Fact]
    public async Task Translate_WhenGeminiFails_ReturnsBadGatewayAndRecordsNothing()
    {
        factory.Gemini.RespondWith = () => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("upstream is down", Encoding.UTF8, "text/plain"),
        };

        var client = factory.CreateClient();
        var token = await RegisterAndLoginViaApiAsync(client, "c@transly.test");

        var response = await TranslateAsync(client, token);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TranslyDbContext>();

        Assert.Equal(0, await db.TranslationUsages.CountAsync());
        Assert.Equal(0, await db.CachedTranslations.CountAsync());
    }

    // ── الأدوات ──────────────────────────────────────────────

    private static object AnyTranslation() => new
    {
        text = "Hello world",
        sourceLanguage = "en",
        targetLanguage = "ar",
        tone = "casual",
    };

    private static async Task<string> RegisterAndLoginViaApiAsync(HttpClient client, string email)
    {
        var register = await client.PostAsJsonAsync("/v1/auth/register", new
        {
            email,
            userName = email.Split('@')[0] + "-user",
            password = Password,
        });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/v1/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();

        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        return body!.AccessToken;
    }

    private static async Task<HttpResponseMessage> TranslateAsync(HttpClient client, string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/translations")
        {
            Content = JsonContent.Create(AnyTranslation()),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await client.SendAsync(request);
    }

    private static HttpResponseMessage GeminiCompleted(string translatedText)
    {
        var body = $$"""
        {
          "id": "int_test",
          "status": "completed",
          "model": "{{TranslyApiFactory.TestModel}}",
          "steps": [
            {
              "type": "model_output",
              "status": "completed",
              "content": [ { "type": "text", "text": "{{translatedText}}" } ]
            }
          ]
        }
        """;

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
    }
}
