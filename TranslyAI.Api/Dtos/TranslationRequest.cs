using System.ComponentModel.DataAnnotations;
using TranslyAI.Api.Enums;

namespace TranslyAI.Api.Dtos;

public record TranslationRequest
{
    public required string Text { get; init; }
    public required string SourceLanguage { get; init; }
    public required string TargetLanguage { get; init; }
    [EnumDataType(typeof(TranslationTone), ErrorMessage = "Invalid Tone value.")]
    public TranslationTone Tone { get; init; } = TranslationTone.Formal;
}