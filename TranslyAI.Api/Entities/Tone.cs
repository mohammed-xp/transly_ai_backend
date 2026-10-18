using TranslyAI.Api.Common;

namespace TranslyAI.Api.Entities;

public class Tone
{
    private readonly List<ToneTranslation> _translations = [];

    private Tone(string code, string promptInstruction)
    {
        Code = code;
        PromptInstruction = promptInstruction;
        PromptVersion = 1;
        IsListed = true;
    }

    public Tone(string code, string name, string description, string promptInstruction)
        : this(code, promptInstruction)
    {
        _translations.Add(new ToneTranslation(AppCultures.Default, name, description));
    }

    public int Id { get; private set; }
    public string Code { get; private set; }
    public string PromptInstruction { get; private set; }
    public int PromptVersion { get; private set; }
    public bool IsListed { get; set; }
    public bool? IsDefault { get; set; }
    public int SortOrder { get; set; }
    public IReadOnlyCollection<ToneTranslation> Translations => _translations;

    public void ChangePrompt(string instruction)
    {
        if (instruction == PromptInstruction)
        {
            return;
        }

        PromptInstruction = instruction;
        PromptVersion++;
    }

    public void SetTranslation(string culture, string name, string description)
    {
        var existing = _translations.FirstOrDefault(t => t.Culture == culture);

        if (existing is null)
        {
            _translations.Add(new ToneTranslation(culture, name, description));
            return;
        }

        existing.Name = name;
        existing.Description = description;
    }

    public void RemoveTranslation(string culture)
    {
        if (culture == AppCultures.Default)
        {
            throw new InvalidOperationException($"The '{AppCultures.Default}' translation is required and cannot be removed.");
        }

        _translations.RemoveAll(t => t.Culture == culture);
    }
}
