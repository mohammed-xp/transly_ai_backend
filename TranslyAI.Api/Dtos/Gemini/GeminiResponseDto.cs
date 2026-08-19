using System.Text.Json.Serialization;

namespace TranslyAI.Api.Dtos.Gemini;

public class GeminiResponseDto
{
    [JsonPropertyName("candidates")]
    public required List<Candidate> Candidates { get; set; }
    [JsonPropertyName("modelVersion")]
    public required string ModelVersion { get; set; }
}

public class Candidate
{
    [JsonPropertyName("content")]
    public required GeminiResponseContent Content { get; set; }
}

public class GeminiResponseContent
{
    [JsonPropertyName("parts")]
    public required List<GeminiResponsePart> Parts { get; set; }
}

public class GeminiResponsePart
{
    [JsonPropertyName("text")]
    public required string Text { get; set; }
}