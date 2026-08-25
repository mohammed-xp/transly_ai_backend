using System.ComponentModel.DataAnnotations;
using TranslyAI.Api.Services;

namespace TranslyAI.Api.Validation;

public sealed class SupportedLanguageAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        return value is string code && LanguageCatalog.IsSupported(code);
    }

    public override string FormatErrorMessage(string name)
    {
        return $"'{name}' is not a supported language code.";
    }
}