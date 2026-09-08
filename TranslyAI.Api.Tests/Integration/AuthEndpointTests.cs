using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TranslyAI.Api.Data;
using TranslyAI.Api.Dtos;

namespace TranslyAI.Api.Tests.Integration;

public class AuthEndpointTests(TranslyApiFactory factory)
    : IClassFixture<TranslyApiFactory>, IAsyncLifetime
{
    private const string Password = "Passw0rd!123";
    private const string Email = "profile@transly.test";

    public async Task InitializeAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TranslyDbContext>();

        await db.TranslationUsages.ExecuteDeleteAsync();
        await db.Users.ExecuteDeleteAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── الـ login بيرجّع بيانات المستخدم، و /me بترجّع نفسها بالظبط ──

    [Fact]
    public async Task Login_ReturnsTheSameUserPayloadAsMe()
    {
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/v1/auth/register", new
        {
            email = Email,
            userName = "profile-user",
            password = Password,
        });

        var login = await client.PostAsJsonAsync("/v1/auth/login", new { email = Email, password = Password });
        var loginBody = await login.Content.ReadFromJsonAsync<LoginResponse>();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.AccessToken);

        var me = await client.SendAsync(request);
        var meBody = await me.Content.ReadFromJsonAsync<UserResponse>();

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal(Email, meBody!.Email);
        Assert.Equal(loginBody.User, meBody);
    }

    // ── توكن سليم لمستخدم اتمسح = 401، مش 404 ──

    [Fact]
    public async Task Me_WhenTheUserWasDeleted_RejectsTheToken()
    {
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/v1/auth/register", new
        {
            email = Email,
            userName = "profile-user",
            password = Password,
        });

        var login = await client.PostAsJsonAsync("/v1/auth/login", new { email = Email, password = Password });
        var loginBody = await login.Content.ReadFromJsonAsync<LoginResponse>();

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TranslyDbContext>();
            await db.Users.ExecuteDeleteAsync();
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.AccessToken);

        var me = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }
}
