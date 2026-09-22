using System.ComponentModel.DataAnnotations;
using TranslyAi_API.Common.Constants;

namespace TranslyAi_API.Validation
{
    public class SupportedLanguageAttribute : ValidationAttribute
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
}
