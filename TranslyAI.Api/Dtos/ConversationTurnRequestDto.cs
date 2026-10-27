
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using TranslyAI.Api.Common;
using TranslyAI.Api.Validation;

namespace TranslyAI.Api.Dtos;

public sealed class ConversationTurnRequestDto : IValidatableObject
{
    public const int MaxAudioBytes = 2 * 1024 * 1024;
    public const int MaxHistoryItems = 4;
    public const int MaxHistoryTextLength = 500;

    // private static readonly JsonSerializerOptions HistoryJsonOptions = new(JsonSerializerDefaults.Web);

    [Required]
    public IFormFile? Audio { get; set; }

    [SupportedLanguage]
    public string? LanguageA { get; set; }

    [SupportedLanguage]
    public string? LanguageB { get; set; }

    [MaxLength(MaxHistoryItems, ErrorMessage = "History can contain at most {1} items.")]
    public List<ConversationHistoryItemDto> History { get; set; } = [];


    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Audio is not null)
        {
            if (Audio.Length == 0)
            {
                yield return new ValidationResult("The audio file is empty.", [nameof(Audio)]);
            }
            else if (Audio.Length > MaxAudioBytes)
            {
                yield return new ValidationResult(
                    $"The audio file must not exceed {MaxAudioBytes / (1024 * 1024)} MB.",
                    [nameof(Audio)]);
            }
        }

        if (LanguageCatalog.IsSupported(LanguageA)
            && LanguageCatalog.IsSupported(LanguageB)
            && LanguageCatalog.Get(LanguageA!).Code == LanguageCatalog.Get(LanguageB!).Code)
        {
            yield return new ValidationResult(
                "LanguageA and LanguageB must be different.",
                [nameof(LanguageB)]);
        }
    }

}
