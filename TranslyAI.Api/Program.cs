using System.Text.Json;
using System.Text.Json.Serialization;
using TranslyAI.Api.AppSettings;
using TranslyAI.Api.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false)
        );
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.Configure<GeminiOptions>(
    builder.Configuration.GetSection("Gemini")
);


builder.Services.AddHttpClient<GeminiApiService>();

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
