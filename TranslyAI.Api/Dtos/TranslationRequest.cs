public record TranslationRequest
{
    public required string Text { get; init; }
    public required string SourceLanguage { get; init; }
    public required string TargetLanguage { get; init; }
    public string Tone { get; init; } = "formal";
}