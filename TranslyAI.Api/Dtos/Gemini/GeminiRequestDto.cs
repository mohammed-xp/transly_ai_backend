namespace TranslyAI.Api.Dtos.Gemini;


public class GeminiRequestDto
{
    public required List<GeminiRequestContent> Contents { get; set; }
    public GenerationConfig GenerationConfig { get; set; } = new GenerationConfig();

}

public class GeminiRequestContent
{
    public required List<GeminiRequestPart> Parts { get; set; }
}

public class GeminiRequestPart
{
    public required string Text { get; set; }
}


public class GenerationConfig
{
    public double Temperature { get; set; } = 0.2;
    public ThinkingConfig ThinkingConfig { get; set; } = new ThinkingConfig();
}

public class ThinkingConfig
{
    public int ThinkingBudget { get; set; } = 0;
}