using System.Text.Json.Serialization;

namespace TranslyAI.Api.Dtos.Gemini;

public sealed record ConversationTurnPayload
{
    // "ok" | "no_speech" | "unsupported_language"
    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("source_language")]
    public string? SourceLanguage { get; init; }

    [JsonPropertyName("transcript")]
    public string? Transcript { get; init; }

    [JsonPropertyName("translation")]
    public string? Translation { get; init; }
}