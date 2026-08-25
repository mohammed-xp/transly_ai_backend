using System.Text.Json;
using System.Text.Json.Serialization;

namespace TranslyAI.Api.Dtos.Gemini;

public class GeminiResponseDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    [JsonPropertyName("status")]
    public string? Status { get; set; }
    [JsonPropertyName("model")]
    public string? Model { get; set; }
    [JsonPropertyName("steps")]
    public List<InteractionStep>? Steps { get; set; }

    [JsonPropertyName("errors")]
    public List<JsonElement>? Errors { get; set; }
}

public class InteractionStep
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    [JsonPropertyName("status")]
    public string? Status { get; set; }
    [JsonPropertyName("content")]
    public List<InteractionContent>? Content { get; set; }
}

public class InteractionContent
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}