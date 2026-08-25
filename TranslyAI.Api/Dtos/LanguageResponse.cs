namespace TranslyAI.Api.Dtos;

public record LanguageResponse
{
    public required IReadOnlyList<Language> Languages { get; init; }
}