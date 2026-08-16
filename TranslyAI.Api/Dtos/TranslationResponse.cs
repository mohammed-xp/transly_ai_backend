public record TranslationResponse
{
    public string SourceText { get; set; } = default!;
    public string TranslatedText { get; set; } = default!;
    public string SourceLanguage { get; set; } = default!;
    public string TargetLanguage { get; set; } = default!;
    public string Tone { get; set; } = default!;
    public string Model { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}