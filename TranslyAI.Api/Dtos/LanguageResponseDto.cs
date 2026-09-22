using TranslyAI.Api.Common;

namespace TranslyAI.Api.Dtos;

public record LanguageResponseDto
{
    public required IReadOnlyList<Language> Languages { get; init; }
}