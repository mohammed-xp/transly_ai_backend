using TranslyAI.Api.Common;
using TranslyAI.Api.Enums;

namespace TranslyAI.Api.Dtos;

public sealed record ConversationTurnResponseDto
{
    public required ConversationTurnStatus Status { get; init; }

    public LanguageDto? SourceLanguage { get; init; }
    public LanguageDto? TargetLanguage { get; init; }
    public string? Transcript { get; init; }
    public string? Translation { get; init; }

    public required int AudioSeconds { get; init; }

    public required string Model { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}