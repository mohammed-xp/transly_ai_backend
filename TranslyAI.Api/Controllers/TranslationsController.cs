using Microsoft.AspNetCore.Mvc;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Enums;
using TranslyAI.Api.Services;

namespace TranslyAI.Api.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class TranslationsController(GeminiApiService geminiApiService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Translate(TranslationRequest translation, CancellationToken cancellationToken)
    {
        var response = await geminiApiService.TranslateAsync(
            translation,
            cancellationToken
        );

        switch (response)
        {
            case TranslationOutcome.Success success:
                return Ok(new TranslationResponse
                {
                    SourceText = translation.Text,
                    TranslatedText = success.Text,
                    SourceLanguage = translation.SourceLanguage,
                    TargetLanguage = translation.TargetLanguage,
                    Model = success.ModelVersion,
                    Tone = translation.Tone,
                    CreatedAt = DateTimeOffset.UtcNow,
                });
            case TranslationOutcome.RateLimited rateLimited:
                if (rateLimited.RetryAfter is not null)
                    Response.Headers.RetryAfter = ((int)rateLimited.RetryAfter.Value.TotalSeconds).ToString();
                return Problem(
                    detail: "The translation request was rate limited. Please try again later.",
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Service Unavailable"
                );
            case TranslationOutcome.InvalidRequest:
                return Problem(
                    detail: "The translation request was invalid. Please check the request parameters.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error"
                );
            case TranslationOutcome.UpstreamError:
                return Problem(
                    detail: "An error occurred while processing the translation request.",
                    statusCode: StatusCodes.Status502BadGateway,
                    title: "Bad Gateway"
                );
            case TranslationOutcome.NotCompleted notCompleted
                    when notCompleted.FinishReason is GeminiFinishReason.SAFETY or GeminiFinishReason.RECITATION:
                return Problem(
                    detail: $"The translation was not completed. Finish reason: {notCompleted.FinishReason}.",
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "Unprocessable Entity"
                );
            case TranslationOutcome.NotCompleted notCompleted:
                return Problem(
                    detail: $"The translation was not completed. Finish reason: {notCompleted.FinishReason}.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error"
                );
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(response),
                    response,
                    "Unhandled translation outcome.");
        }
    }
}