using TranslyAI.Api.Common;

namespace TranslyAI.Api.Dtos;

public record LanguageResponseDto
{
    public required IReadOnlyList<LanguageDto> Languages { get; init; }
}