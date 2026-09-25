using System.Collections.Frozen;

namespace TranslyAI.Api.Common;

public static class LanguageCatalog
{
    public static IReadOnlyList<LanguageDto> All { get; } =
    [
        new LanguageDto { Code = "ar", Name = "Arabic",   NativeName = "العربية", IsRtl = true },
        new LanguageDto { Code = "en", Name = "English",  NativeName = "English" },
        new LanguageDto { Code = "fr", Name = "French",   NativeName = "Français" },
        new LanguageDto { Code = "de", Name = "German",   NativeName = "Deutsch" },
        new LanguageDto { Code = "es", Name = "Spanish",  NativeName = "Español" },
        new LanguageDto { Code = "tr", Name = "Turkish",  NativeName = "Türkçe" },
        new LanguageDto { Code = "ru", Name = "Russian",  NativeName = "Русский" },
        new LanguageDto { Code = "zh", Name = "Chinese",  NativeName = "中文" },
        new LanguageDto { Code = "ja", Name = "Japanese", NativeName = "日本語" },
        new LanguageDto { Code = "hi", Name = "Hindi",    NativeName = "हिन्दी" },
        new LanguageDto { Code = "ur", Name = "Urdu",     NativeName = "اردو", IsRtl = true },
        new LanguageDto { Code = "he", Name = "Hebrew",   NativeName = "עברית", IsRtl = true },
    ];

    private static readonly FrozenDictionary<string, LanguageDto> ByCode =
        All.ToFrozenDictionary(lan => lan.Code, StringComparer.OrdinalIgnoreCase);

    public static bool IsSupported(string? code) =>
        code is not null && ByCode.ContainsKey(code);


    public static LanguageDto Get(string code) => ByCode[code];
}