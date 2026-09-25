namespace TranslyAI.Api.Common;

public record LanguageDto
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string NativeName { get; init; }
    public bool IsRtl { get; init; }
}