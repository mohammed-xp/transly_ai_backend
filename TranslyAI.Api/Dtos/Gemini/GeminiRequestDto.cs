using System.Text.Json.Serialization;

namespace TranslyAI.Api.Dtos.Gemini;


public record GeminiRequestDto
{
    [JsonPropertyName("input")]
    public required string Input { get; init; }
    [JsonPropertyName("model")]
    public required string Model { get; init; }
    [JsonPropertyName("system_instruction")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SystemInstruction { get; init; }
    [JsonPropertyName("store")]
    public bool Store { get; init; }
}
