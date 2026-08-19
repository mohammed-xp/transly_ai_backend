namespace TranslyAI.Api.AppSettings;

public class GeminiOptions
{
    public required string ApiKey { get; init; } = string.Empty;
    public required string Model { get; init; } = string.Empty;
}