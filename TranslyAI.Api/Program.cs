using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TranslyAI.Api.AppSettings;
using TranslyAI.Api.Data;
using TranslyAI.Api.Entities;
using TranslyAI.Api.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers(options =>
{
    options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
})
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(
                JsonNamingPolicy.CamelCase,
                allowIntegerValues: false)
        );
    });

builder.Services.AddDbContext<TranslyDbContext>(options =>
    options.UseMySQL(
        builder.Configuration.GetConnectionString("Transly")
        ?? throw new InvalidOperationException("Connection string 'Transly' is missing.")
    )
);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// builder.Configuration.GetSection("Gemini")
builder.Services.AddOptions<GeminiOptions>()
    .Bind(builder.Configuration.GetSection("Gemini"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<QuotaOptions>()
    .Bind(builder.Configuration.GetSection("Quota"))
    .ValidateDataAnnotations()
    .ValidateOnStart();


builder.Services.AddHttpClient<GeminiApiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<AuthService>();


builder.Services.AddScoped<TranslationService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
        ?? throw new InvalidOperationException("Jwt configuration section is missing.");

        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,

            ValidateAudience = true,
            ValidAudience = jwt.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),

            ValidateLifetime = true,

            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Jwt Authentication Failed: {Respon}", context.Exception.Message);

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();


app.MapGet("/health", () => new
{
    status = "Healthy",
    time = DateTimeOffset.UtcNow,
    environment = app.Environment.EnvironmentName
});

app.Run();

public partial class Program;
