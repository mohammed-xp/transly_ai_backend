using System.Text.Json;
using System.Text.Json.Serialization;
using TranslyAI.Api.AppSettings;
using TranslyAI.Api.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(
                JsonNamingPolicy.CamelCase,
                allowIntegerValues: false)
        );
    });
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
