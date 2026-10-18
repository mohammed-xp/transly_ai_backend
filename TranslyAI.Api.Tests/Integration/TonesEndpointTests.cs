using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TranslyAI.Api.Data;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Entities;

namespace TranslyAI.Api.Tests.Integration;

public class TonesEndpointTests(TranslyApiFactory factory) : IClassFixture<TranslyApiFactory>
{
    private const string UntranslatedCode = "test-untranslated";

    [Fact]
    public async Task GetTones_WithoutAcceptLanguage_ReturnsEnglish()
    {
        var response = await GetTonesAsync(acceptLanguage: null);

        var body = await ApiResponseAssert.SuccessAsync<ToneListResponseDto>(response);
        Assert.Equal("Formal", Tone(body, "formal").Name);
        Assert.Equal("en", Assert.Single(response.Content.Headers.ContentLanguage));
    }

    [Fact]
    public async Task GetTones_WithArabic_ReturnsArabic()
    {
        var response = await GetTonesAsync("ar");

        var body = await ApiResponseAssert.SuccessAsync<ToneListResponseDto>(response);
        Assert.Equal("رسمي", Tone(body, "formal").Name);
        Assert.Equal("ودّي", Tone(body, "casual").Name);
        Assert.Equal("مختصر", Tone(body, "short").Name);
        Assert.Equal("ar", Assert.Single(response.Content.Headers.ContentLanguage));
    }

    [Fact]
    public async Task GetTones_WithRegionalArabic_FallsBackToParentCulture()
    {
        var response = await GetTonesAsync("ar-SA");

        var body = await ApiResponseAssert.SuccessAsync<ToneListResponseDto>(response);
        Assert.Equal("رسمي", Tone(body, "formal").Name);
    }

    [Fact]
    public async Task GetTones_WithUnsupportedLanguage_ReturnsEnglish()
    {
        var response = await GetTonesAsync("fr");

        var body = await ApiResponseAssert.SuccessAsync<ToneListResponseDto>(response);
        Assert.Equal("Formal", Tone(body, "formal").Name);
        Assert.Equal("en", Assert.Single(response.Content.Headers.ContentLanguage));
    }

    [Fact]
    public async Task GetTones_WhenToneHasNoArabicTranslation_ReturnsEnglishForThatTone()
    {
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TranslyDbContext>();
            db.Tones.Add(new Tone(UntranslatedCode, "Untranslated", "English only.", "Translate normally.") { SortOrder = 99 });
            await db.SaveChangesAsync();
        }

        try
        {
            var response = await GetTonesAsync("ar");

            var body = await ApiResponseAssert.SuccessAsync<ToneListResponseDto>(response);
            Assert.Equal("Untranslated", Tone(body, UntranslatedCode).Name);
            Assert.Equal("English only.", Tone(body, UntranslatedCode).Description);
            Assert.Equal("رسمي", Tone(body, "formal").Name);
        }
        finally
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TranslyDbContext>();
            await db.Tones.Where(t => t.Code == UntranslatedCode).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task GetTones_VariesCacheByAcceptLanguage()
    {
        var response = await GetTonesAsync("ar");

        Assert.Contains("Accept-Language", response.Headers.Vary);
    }

    [Fact]
    public async Task GetTones_CodesAreNotLocalized()
    {
        var english = await ApiResponseAssert.SuccessAsync<ToneListResponseDto>(await GetTonesAsync("en"));
        var arabic = await ApiResponseAssert.SuccessAsync<ToneListResponseDto>(await GetTonesAsync("ar"));

        Assert.Equal(english.DefaultTone, arabic.DefaultTone);
        Assert.Equal(english.Tones.Select(t => t.Code), arabic.Tones.Select(t => t.Code));
    }

    private async Task<HttpResponseMessage> GetTonesAsync(string? acceptLanguage)
    {
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/tones");

        if (acceptLanguage is not null)
        {
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(acceptLanguage));
        }

        return await client.SendAsync(request);
    }

    private static ToneDto Tone(ToneListResponseDto body, string code) =>
        body.Tones.Single(t => t.Code == code);
}
