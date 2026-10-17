namespace TranslyAI.Api.Dtos;

public record ToneDto
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
}
