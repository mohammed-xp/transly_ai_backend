using System.ComponentModel.DataAnnotations;
using TranslyAI.Api.Enums;

namespace TranslyAI.Api.Entities;

public class TranslationUsage
{
    public long Id { get; set; }
    public required Guid UserId { get; set; }
    [MaxLength(8)]
    public required string SourceLanguage { get; set; }
    [MaxLength(8)]
    public required string TargetLanguage { get; set; }
    public required TranslationTone Tone { get; set; }
    public required int CharacterCount { get; set; }
    public required TranslationSource Source { get; set; }
    public required DateTime CreatedAtUtc { get; set; }
}