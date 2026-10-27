using System.ComponentModel.DataAnnotations;
using TranslyAI.Api.Validation;

namespace TranslyAI.Api.Dtos;

public sealed record ConversationHistoryItemDto
{
    [SupportedLanguage]
    public required string Language { get; init; }

    [Required]
    [StringLength(ConversationTurnRequestDto.MaxHistoryTextLength, MinimumLength = 1)]
    public required string Text { get; init; }
}
