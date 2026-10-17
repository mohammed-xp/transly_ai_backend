namespace TranslyAI.Api.Entities;

public class Tone
{
    public Tone(string code, string name, string description, string promptInstruction)
    {
        Code = code;
        Name = name;
        Description = description;
        PromptInstruction = promptInstruction;
        PromptVersion = 1;
        IsListed = true;
    }

    public int Id { get; private set; }
    public string Code { get; private set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string PromptInstruction { get; private set; }
    public int PromptVersion { get; private set; }
    public bool IsListed { get; set; }
    public bool? IsDefault { get; set; }
    public int SortOrder { get; set; }

    public void ChangePrompt(string instruction)
    {
        if (instruction == PromptInstruction)
        {
            return;
        }

        PromptInstruction = instruction;
        PromptVersion++;
    }
}
