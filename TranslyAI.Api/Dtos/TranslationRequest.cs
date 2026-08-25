using System.ComponentModel.DataAnnotations;
using TranslyAI.Api.Enums;
using TranslyAI.Api.Services;
using TranslyAI.Api.Validation;

namespace TranslyAI.Api.Dtos;

public record TranslationRequest : IValidatableObject
{
    public required string Text { get; init; }
    [SupportedLanguage]
    public required string SourceLanguage { get; init; }
    [SupportedLanguage]
    public required string TargetLanguage { get; init; }
    [EnumDataType(typeof(TranslationTone), ErrorMessage = "Invalid Tone value.")]
    public TranslationTone Tone { get; init; } = TranslationTone.Formal;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (LanguageCatalog.IsSupported(SourceLanguage)
            && LanguageCatalog.IsSupported(TargetLanguage)
            && LanguageCatalog.Get(SourceLanguage).Code == LanguageCatalog.Get(TargetLanguage).Code
        )
        {
            yield return new ValidationResult(
                "Source and target languages must be differnt.",
                [nameof(TargetLanguage)]
            );
        }
    }
}