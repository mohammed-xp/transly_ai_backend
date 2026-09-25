using TranslyAI.Api.Common;
using TranslyAI.Api.Enums;

namespace TranslyAI.Api.Dtos;

public record TranslationResponseDto
{
    public required string SourceText { get; init; }
    public required string TranslatedText { get; init; }
    public required LanguageDto SourceLanguage { get; init; }
    public required LanguageDto TargetLanguage { get; init; }
    public required TranslationTone Tone { get; init; }
    public required string Model { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}