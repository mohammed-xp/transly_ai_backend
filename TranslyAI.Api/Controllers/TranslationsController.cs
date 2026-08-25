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
    public async Task<ActionResult<TranslationResponse>> Translate(TranslationRequest translation, CancellationToken cancellationToken)
    {
        var request = translation with
        {
            SourceLanguage = LanguageCatalog.Get(translation.SourceLanguage).Code,
            TargetLanguage = LanguageCatalog.Get(translation.TargetLanguage).Code,
        };
        var outcome = await geminiApiService.TranslateAsync(request, cancellationToken);

        switch (outcome)
        {
            case TranslationOutcome.Success success:
                return Ok(new TranslationResponse
                {
                    SourceText = request.Text,
                    TranslatedText = success.Text,
                    SourceLanguage = request.SourceLanguage,
                    TargetLanguage = request.TargetLanguage,
                    Model = success.ModelVersion,
                    Tone = request.Tone,
                    CreatedAt = DateTimeOffset.UtcNow,
                });

            // 503: الـ quota بتاعتنا خلصت.
            case TranslationOutcome.RateLimited rateLimited:
                if (rateLimited.RetryAfter is not null)
                    Response.Headers.RetryAfter = ((int)rateLimited.RetryAfter.Value.TotalSeconds).ToString();
                return Problem(
                    detail: "The translation service is temporarily unavailable. Please try again later.",
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Service Unavailable"
                );

            // 500: Gemini رفض الطلب بتاعنا (body مش مظبوط أو key مرفوض).
            case TranslationOutcome.InvalidRequest:
                return Problem(
                    detail: "The translation service rejected the request.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error"
                );

            // 504:الـ timeout معناه "جرّب تاني ممكن ينفع"،
            case TranslationOutcome.UpstreamTimeout:
                return Problem(
                    detail: "The translation service did not respond in time.",
                    statusCode: StatusCodes.Status504GatewayTimeout,
                    title: "Gateway Timeout"
                );

            // 502: إحنا gateway قدام Gemini، وGemini مردش أو رد برد مش صالح.
            case TranslationOutcome.UpstreamError:
                return Problem(
                    detail: "The translation service returned an invalid response.",
                    statusCode: StatusCodes.Status502BadGateway,
                    title: "Bad Gateway"
                );

            // 422: الطلب صحيح شكلاً ومفهوم، بس المحتوى نفسه اترفض.
            // دي الحالة الوحيدة هنا اللي العميل يقدر يتصرف فيها — يغيّر النص، فبنقوله السبب.
            case TranslationOutcome.NotCompleted notCompleted
                    when notCompleted.FinishReason is GeminiFinishReason.Safety or GeminiFinishReason.Recitation:
                return Problem(
                    detail: $"The text could not be translated. Reason: {notCompleted.FinishReason}.",
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "Unprocessable Entity"
                );

            // 500: MaxTokens / Other / Unknown — دي مشاكل في إعدادنا إحنا أو قيمة جديدة من Google.
            case TranslationOutcome.NotCompleted:
                return Problem(
                    detail: "The translation could not be completed.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error"
                );

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(outcome),
                    outcome,
                    "Unhandled translation outcome.");
        }
    }
}
