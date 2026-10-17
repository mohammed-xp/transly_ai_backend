using System.ComponentModel.DataAnnotations;

namespace TranslyAI.Api.Entities;

public class CachedTranslation
{
    public int Id { get; set; }

    [MaxLength(64)]
    public required string CacheKey { get; set; }
    [MaxLength(8)]
    public required string SourceLanguage { get; set; }
    [MaxLength(8)]
    public required string TargetLanguage { get; set; }
    public required int ToneId { get; set; }
    public required int PromptVersion { get; set; }
    public required string SourceText { get; set; }
    public required string TranslatedText { get; set; }
    [MaxLength(128)]
    public required string Model { get; set; }
    public required DateTime CreatedAtUtc { get; set; }
}