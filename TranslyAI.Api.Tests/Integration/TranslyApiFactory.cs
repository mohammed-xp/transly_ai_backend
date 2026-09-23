using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TranslyAI.Api.Data;
using TranslyAI.Api.Services;
using TranslyAI.Api.Services.IServices;
using TranslyAI.Api.Tests.Fakes;

namespace TranslyAI.Api.Tests.Integration;

public sealed class TranslyApiFactory : WebApplicationFactory<Program>
{
    public const string TestModel = "test-model";

    public GeminiStubHandler Gemini { get; } = new();

    public const int RequestsPerDay = 2;


    public TranslyApiFactory()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TranslyDbContext>();

        db.Database.EnsureDeleted();
        db.Database.Migrate();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Transly"] = ResolveTestConnectionString(),

                ["Gemini:ApiKey"] = "not-a-real-key",
                ["Gemini:Model"] = TestModel,

                ["Jwt:Issuer"] = "transly-tests",
                ["Jwt:Audience"] = "transly-tests",
                ["Jwt:SigningKey"] = "integration-tests-signing-key-0123456789",
                ["Jwt:AccessTokenMinutes"] = "5",
                ["Quota:RequestsPerDay"] = RequestsPerDay.ToString(CultureInfo.InvariantCulture),
            });
        });

                builder.ConfigureTestServices(services =>
        {
            services.AddHttpClient<IGeminiApiService, GeminiApiService>()
                .ConfigurePrimaryHttpMessageHandler(() => Gemini);
        });

    }

    private static string ResolveTestConnectionString()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<TranslyApiFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        return configuration.GetConnectionString("TranslyTests")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:TranslyTests is missing. Set it in the test project's user secrets.");
    }
}
