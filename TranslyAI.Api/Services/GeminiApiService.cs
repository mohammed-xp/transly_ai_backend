using System.Net;
using Microsoft.Extensions.Options;
using TranslyAI.Api.AppSettings;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Dtos.Gemini;
using TranslyAI.Api.Enums;

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
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
    }


    public async Task<TranslationOutcome> TranslateAsync(TranslationRequest translationRequest, CancellationToken cancellationToken)
    {
        string toneInstruction = translationRequest.Tone switch
        {
            TranslationTone.Formal => "Use a formal, professional register.",
            TranslationTone.Casual => "Use a relaxed, conversational register.",
            TranslationTone.Short => "Translate as briefly as possible while preserving meaning.",
            _ => throw new ArgumentOutOfRangeException(
                paramName: nameof(translationRequest.Tone),
                actualValue: translationRequest.Tone,
                message: "Invalid tone value."
            ),
        };
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
            return httpResponse.StatusCode switch
            {
                HttpStatusCode.TooManyRequests => new TranslationOutcome.RateLimited(
                        RetryAfter: httpResponse.Headers.RetryAfter?.Delta
                    ),
                HttpStatusCode.BadRequest => new TranslationOutcome.InvalidRequest(),
                HttpStatusCode.Forbidden => new TranslationOutcome.InvalidRequest(),
                _ => new TranslationOutcome.UpstreamError(),
            };
        }

        var response = await httpResponse.Content.ReadFromJsonAsync<GeminiResponseDto>(cancellationToken);

        var candidate = response?.Candidates.FirstOrDefault();

        if (response is null || candidate is null)
        {
            _logger.LogError("Gemini returned an empty response.");
            return new TranslationOutcome.UpstreamError();
        }

        if (candidate.FinishReason != GeminiFinishReason.STOP)
        {
            _logger.LogError("Gemini returned an incomplete response. FinishReason: {FinishReason}", candidate.FinishReason);
            return new TranslationOutcome.NotCompleted(
                FinishReason: candidate.FinishReason
            );
        }

        var translatedText = candidate.Content.Parts.FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(translatedText)
            || string.IsNullOrWhiteSpace(response.ModelVersion)
            )
        {
            _logger.LogError("Gemini returned an empty translation.");
            return new TranslationOutcome.UpstreamError();
        }


        return new TranslationOutcome.Success(
            translatedText,
            response.ModelVersion
            );
    }



}