using System.Text.Json.Serialization;

namespace TranslyAI.Api.Dtos.Gemini;

public class GeminiResponseDto
{
    [JsonPropertyName("candidates")]
    public List<Candidate>? Candidates { get; set; }
    [JsonPropertyName("modelVersion")]
    public string? ModelVersion { get; set; }
}

public class Candidate
{
    [JsonPropertyName("content")]
    public GeminiResponseContent? Content { get; set; }
    [JsonPropertyName("finishReason")]
    public string? FinishReason { get; set; }
}

public class GeminiResponseContent
{
    [JsonPropertyName("parts")]
    public List<GeminiResponsePart>? Parts { get; set; }
}

public class GeminiResponsePart
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}