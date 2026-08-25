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
            Input = prompt,
            Model = _options.Model,
            Store = false,
        };

        HttpResponseMessage httpResponse;
        try
        {
            httpResponse = await _httpClient.PostAsJsonAsync(
                "v1beta/interactions",
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

            var status = MapStatus(response.Status);

            if (status == GeminiInteractionStatus.Unknown)
            {
                _logger.LogWarning(
                    "Gemini returned an unrecognised status: {Raw}. InteractionStatus may need a new member.",
                    response.Status);
            }

            if (status != GeminiInteractionStatus.Completed)
            {
                _logger.LogError(
                   "Interaction did not complete. status: {Status}, errors: {Errors}",
                   response.Status,
                   response.Errors is null ? "(none)" : JsonSerializer.Serialize(response.Errors));

                return status switch
                {
                    GeminiInteractionStatus.BudgetExceeded => new TranslationOutcome.RateLimited(RetryAfter: null),
                    GeminiInteractionStatus.RequiresAction => new TranslationOutcome.InvalidRequest(),
                    _ => new TranslationOutcome.NotCompleted(status),

                };
            }

            var translatedText = ExtractText(response.Steps);

            if (string.IsNullOrWhiteSpace(translatedText) || string.IsNullOrWhiteSpace(response.Model))
            {
                _logger.LogError(
                    "Interaction completed but returned no usable text or model."
                );
                return new TranslationOutcome.UpstreamError();
            }

            return new TranslationOutcome.Success(translatedText, response.Model);
        }
    }

    private static string? ExtractText(List<InteractionStep>? steps)
    {
        if (steps is null)
        {
            return null;
        }

        var text = string.Concat(
            steps.Where(step => step.Type == "model_output")
                .SelectMany(step => step.Content ?? [])
                .Where(item => item.Type == "text" && !string.IsNullOrEmpty(item.Text))
                .Select(item => item.Text!)
        );

        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static GeminiInteractionStatus MapStatus(string? raw) => raw switch
    {
        "queued" => GeminiInteractionStatus.Queued,
        "in_progress" => GeminiInteractionStatus.InProgress,
        "requires_action" => GeminiInteractionStatus.RequiresAction,
        "completed" => GeminiInteractionStatus.Completed,
        "failed" => GeminiInteractionStatus.Failed,
        "cancelled" => GeminiInteractionStatus.Cancelled,
        "incomplete" => GeminiInteractionStatus.Incomplete,
        "budget_exceeded" => GeminiInteractionStatus.BudgetExceeded,
        _ => GeminiInteractionStatus.Unknown,
    };
}
