using Microsoft.Extensions.Options;
using TranslyAI.Api.AppSettings;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Dtos.Gemini;

namespace TranslyAI.Api.Services;


public class GeminiApiService
{
    private readonly ILogger<GeminiApiService> _logger;
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;

    public GeminiApiService(ILogger<GeminiApiService> logger, HttpClient httpClient, IOptions<GeminiOptions> geminiOptions)
    {
        _logger = logger;
        _httpClient = httpClient;
        _options = geminiOptions.Value;

        _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
        _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", _options.ApiKey);
    }


    public async Task<GeminiTranslationResult?> TranslateAsync(TranslationRequest translationRequest, CancellationToken cancellationToken)
    {
        string toneInstruction;
        switch (translationRequest.Tone)
        {
            case "formal":
                toneInstruction = "Use a formal, professional register.";
                break;
            case "casual":
                toneInstruction = "Use a relaxed, conversational register.";
                break;
            case "short":
                toneInstruction = "Translate as briefly as possible while preserving meaning.";
                break;
            default:
                toneInstruction = "";
                break;

        }
        string prompt = $"""
        You are a translation engine. Translate the following text from {translationRequest.SourceLanguage} to {translationRequest.TargetLanguage}.
        {toneInstruction}
        Respond with ONLY the translated text — no preamble, no explanations, no quotation marks, no markdown.

        Text: {translationRequest.Text}
        """;

        var geminiRequestDto = new GeminiRequestDto
        {
            Contents = [
                new GeminiRequestContent
            {
                Parts = [
                    new GeminiRequestPart
                    {
                        Text = prompt,
                    }
                ]
            }
            ]
        };

        var httpResponse = await _httpClient.PostAsJsonAsync(
            $"v1beta/models/{_options.Model}:generateContent",
            geminiRequestDto,
            cancellationToken
        );

        if (!httpResponse.IsSuccessStatusCode)
        {
            var errorBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Gemini returned {Status}: {Body}", (int)httpResponse.StatusCode, errorBody);
            return null;
        }

        var response = await httpResponse.Content.ReadFromJsonAsync<GeminiResponseDto>(cancellationToken);

        var translatedText = response?.Candidates.FirstOrDefault()?.Content.Parts.FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(translatedText) || string.IsNullOrWhiteSpace(response?.ModelVersion))
        {
            _logger.LogError("Gemini returned an empty translation.");
            return null;
        }

        var geminiTranslationResult = new GeminiTranslationResult(
            translatedText,
            response.ModelVersion
            );

        return geminiTranslationResult;
    }



}