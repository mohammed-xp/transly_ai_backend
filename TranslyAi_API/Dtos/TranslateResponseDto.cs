using TranslyAi_API.Enums;

namespace TranslyAi_API.Dtos
{
    public record TranslateResponseDto
    {
        public required string SourceText { get; init; }
        public required string TranslatedText { get; init; }
        public required string SourceLanguage { get; init; }
        public required string TargetLanguage { get; init; }
        public required TranslationTone Tone { get; init; }
        public required string Model { get; init; }
        public required DateTime CreatedAt { get; init; }
    }
}
