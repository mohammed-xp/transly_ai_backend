using System.Net;
using System.Text.Json;
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

        HttpResponseMessage httpResponse;
        try
        {
            httpResponse = await _httpClient.PostAsJsonAsync(
                $"v1beta/models/{_options.Model}:generateContent",
                geminiRequestDto,
                cancellationToken
            );
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Gemini did not respond within {Timeout}.", _httpClient.Timeout);
            return new TranslationOutcome.UpstreamTimeout();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Could not reach Gemini.");
            return new TranslationOutcome.UpstreamError();
        }

        using (httpResponse)
        {

            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini returned {Status}: {Body}", (int)httpResponse.StatusCode, errorBody);

                return httpResponse.StatusCode switch
                {
                    HttpStatusCode.TooManyRequests => new TranslationOutcome.RateLimited(
                        RetryAfter: httpResponse.Headers.RetryAfter?.Delta
                    ),
                    HttpStatusCode.BadRequest
                        or HttpStatusCode.Unauthorized
                        or HttpStatusCode.Forbidden => new TranslationOutcome.InvalidRequest(),
                    _ => new TranslationOutcome.UpstreamError(),
                };
            }

            GeminiResponseDto? response;
            try
            {
                response = await httpResponse.Content.ReadFromJsonAsync<GeminiResponseDto>(cancellationToken);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Gemini returned a body that is not valid JSON.");
                return new TranslationOutcome.UpstreamError();
            }

            if (response is null)
            {
                _logger.LogError("Gemini returned an empty body.");
                return new TranslationOutcome.UpstreamError();
            }

            var candidate = response.Candidates?.FirstOrDefault();

            if (candidate is null)
            {
                _logger.LogError("Gemini returned 200 with no candidates — the prompt was most likely blocked.");
                return new TranslationOutcome.UpstreamError();
            }

            var finishReason = MapFinishReason(candidate.FinishReason);

            if (finishReason == GeminiFinishReason.Unknown)
            {
                _logger.LogWarning(
                    "Gemini returned an unrecognised finishReason: {Raw}. GeminiFinishReason may need a new member.",
                    candidate.FinishReason);
            }

            if (finishReason != GeminiFinishReason.Stop)
            {
                _logger.LogError("Gemini stopped early. finishReason: {Raw}", candidate.FinishReason);
                return new TranslationOutcome.NotCompleted(finishReason);
            }

            var translatedText = candidate.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrWhiteSpace(translatedText) || string.IsNullOrWhiteSpace(response.ModelVersion))
            {
                _logger.LogError("Gemini returned STOP but no usable text or modelVersion.");
                return new TranslationOutcome.UpstreamError();
            }
            return new TranslationOutcome.Success(translatedText, response.ModelVersion);
        }
    }

    private static GeminiFinishReason MapFinishReason(string? raw) => raw switch
    {
        "STOP" => GeminiFinishReason.Stop,
        "MAX_TOKENS" => GeminiFinishReason.MaxTokens,
        "SAFETY" => GeminiFinishReason.Safety,
        "RECITATION" => GeminiFinishReason.Recitation,
        "OTHER" => GeminiFinishReason.Other,
        _ => GeminiFinishReason.Unknown,
    };
}
