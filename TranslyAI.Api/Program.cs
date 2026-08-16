var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseHttpsRedirection();


app.MapControllers();


app.MapGet("/health", () => new
{
    status = "Healthy",
    time = DateTimeOffset.UtcNow,
    environment = app.Environment.EnvironmentName
});

app.Run();
