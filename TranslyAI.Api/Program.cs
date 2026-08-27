using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using TranslyAI.Api.AppSettings;
using TranslyAI.Api.Data;
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


builder.Services.AddHttpClient<GeminiApiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});


builder.Services.AddScoped<TranslationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.MapControllers();


app.MapGet("/health", () => new
{
    status = "Healthy",
    time = DateTimeOffset.UtcNow,
    environment = app.Environment.EnvironmentName
});

app.Run();
