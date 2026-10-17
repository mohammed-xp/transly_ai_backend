using System.ComponentModel.DataAnnotations;
using TranslyAI.Api.Common;
using TranslyAI.Api.Validation;

namespace TranslyAI.Api.Dtos;

public record TranslationRequestDto : IValidatableObject
{
    public required string Text { get; init; }
    [SupportedLanguage]
    public required string SourceLanguage { get; init; }
    [SupportedLanguage]
    public required string TargetLanguage { get; init; }
    [MaxLength(32)]
    public string? Tone { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (LanguageCatalog.IsSupported(SourceLanguage)
            && LanguageCatalog.IsSupported(TargetLanguage)
            && LanguageCatalog.Get(SourceLanguage).Code == LanguageCatalog.Get(TargetLanguage).Code
        )
        {
            yield return new ValidationResult(
                "Source and target languages must be differnt.",
                ["TargetLanguage"]
            );
        }
    }
}