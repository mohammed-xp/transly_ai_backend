using System.ComponentModel.DataAnnotations;

namespace TranslyAI.Api.AppSettings;

public class GeminiOptions
{
    [Required(ErrorMessage = "Gemini ApiKey is required.")]
    public required string ApiKey { get; init; } = string.Empty;
    [Required(ErrorMessage = "Gemini Model is required.")]
    public required string Model { get; init; } = string.Empty;
}