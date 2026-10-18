namespace TranslyAI.Api.Entities;

public class ToneTranslation
{
    public ToneTranslation(string culture, string name, string description)
    {
        Culture = culture;
        Name = name;
        Description = description;
    }

    public int ToneId { get; private set; }
    public string Culture { get; private set; }
    public string Name { get; set; }
    public string Description { get; set; }
}
