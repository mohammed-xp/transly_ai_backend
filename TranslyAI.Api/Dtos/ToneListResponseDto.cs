namespace TranslyAI.Api.Dtos;

public record ToneListResponseDto
{
    public required string DefaultTone { get; init; }
    public required IReadOnlyList<ToneDto> Tones { get; init; }
}
