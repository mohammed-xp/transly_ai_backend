namespace TranslyAI.Api.Dtos.Gemini;

public record GeminiTranslationResult(
    string Text,
    string ModelVersion
);