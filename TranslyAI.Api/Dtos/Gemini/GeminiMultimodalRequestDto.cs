using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace TranslyAI.Api.Dtos.Gemini;

public record GeminiMultimodalRequestDto
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("input")]
    public required IReadOnlyList<GeminiInputContent> Input { get; init; }

    [JsonPropertyName("system_instruction")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SystemInstruction { get; init; }

    [JsonPropertyName("response_format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GeminiResponseFormat? ResponseFormat { get; init; }

    [JsonPropertyName("store")]
    public bool Store { get; init; }
}

public record GeminiInputContent
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; init; }

    // الصوت بـ base64
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Data { get; init; }

    [JsonPropertyName("mime_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MimeType { get; init; }

    public static GeminiInputContent FromText(string text) => new()
    {
        Type = "text",
        Text = text,
    };

    public static GeminiInputContent FromAudio(byte[] audio, string mimeType) => new()
    {
        Type = "audio",
        Data = Convert.ToBase64String(audio),
        MimeType = mimeType,
    };
}

public record GeminiResponseFormat
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "text";

    [JsonPropertyName("mime_type")]
    public string MimeType { get; init; } = "application/json";

    // JSON Schema — بنبنيه في الـ service عشان الـ enum بتاع اللغات بيتغير مع كل request
    [JsonPropertyName("schema")]
    public required JsonNode Schema { get; init; }
}