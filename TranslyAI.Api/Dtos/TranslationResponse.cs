namespace TranslyAI.Api.Dtos;

public record TranslationResponse
{
    public required string SourceText { get; init; }
    public required string TranslatedText { get; init; }
    public required string SourceLanguage { get; init; }
    public required string TargetLanguage { get; init; }
    public required string Tone { get; init; }
    public required string Model { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}