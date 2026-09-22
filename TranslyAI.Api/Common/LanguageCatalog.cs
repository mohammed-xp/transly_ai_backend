using System.Collections.Frozen;

namespace TranslyAI.Api.Common;

public static class LanguageCatalog
{
    public static IReadOnlyList<Language> All { get; } =
    [
        new Language { Code = "ar", Name = "Arabic",   NativeName = "العربية", IsRtl = true },
        new Language { Code = "en", Name = "English",  NativeName = "English" },
        new Language { Code = "fr", Name = "French",   NativeName = "Français" },
        new Language { Code = "de", Name = "German",   NativeName = "Deutsch" },
        new Language { Code = "es", Name = "Spanish",  NativeName = "Español" },
        new Language { Code = "tr", Name = "Turkish",  NativeName = "Türkçe" },
        new Language { Code = "ru", Name = "Russian",  NativeName = "Русский" },
        new Language { Code = "zh", Name = "Chinese",  NativeName = "中文" },
        new Language { Code = "ja", Name = "Japanese", NativeName = "日本語" },
        new Language { Code = "hi", Name = "Hindi",    NativeName = "हिन्दी" },
        new Language { Code = "ur", Name = "Urdu",     NativeName = "اردو", IsRtl = true },
        new Language { Code = "he", Name = "Hebrew",   NativeName = "עברית", IsRtl = true },
    ];

    private static readonly FrozenDictionary<string, Language> ByCode =
        All.ToFrozenDictionary(lan => lan.Code, StringComparer.OrdinalIgnoreCase);

    public static bool IsSupported(string? code) =>
        code is not null && ByCode.ContainsKey(code);


    public static Language Get(string code) => ByCode[code];
}