using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TranslyAI.Api.Common;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Enums;
using TranslyAI.Api.Extensions;
using TranslyAI.Api.Services;
using TranslyAI.Api.Services.IServices;

namespace TranslyAI.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class TranslationsController(ITranslationService translationService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ApiResponse<TranslationResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponse<TranslationResponseDto>>> Translate(
        TranslationRequestDto translation,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var request = translation with
        {
            SourceLanguage = LanguageCatalog.Get(translation.SourceLanguage).Code,
            TargetLanguage = LanguageCatalog.Get(translation.TargetLanguage).Code,
        };
        var result = await translationService.TranslateAsync(request, userId.Value, cancellationToken);

        Response.Headers["X-RateLimit-Limit"] = result.Quota.Limit.ToString(CultureInfo.InvariantCulture);
        Response.Headers["X-RateLimit-Remaining"] = result.Quota.Remaining.ToString(CultureInfo.InvariantCulture);
        Response.Headers["X-RateLimit-Reset"] = result.Quota.ResetsAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

        switch (result.Outcome)
        {
            case TranslationOutcome.Success success:
                return Ok(new TranslationResponseDto
                {
                    SourceText = request.Text,
                    TranslatedText = success.Text,
                    SourceLanguage = request.SourceLanguage,
                    TargetLanguage = request.TargetLanguage,
                    Model = success.Model,
                    Tone = request.Tone,
                    CreatedAt = success.CreatedAt,
                });
            // 429: الـ quota الخاصة بالمستخدم خلصت.
            case TranslationOutcome.QuotaExceeded:
                Response.Headers.RetryAfter = SecondsUntil(result.Quota.ResetsAt);
                return StatusCode(
                    429,
                    ApiResponse<object>.TooManyRequests("You have used your daily translation quota")
                );
            //return Problem(
            //    detail: "Youhave used your daily translation quota.",
            //    statusCode: StatusCodes.Status429TooManyRequests,
            //    title: "Too Many Requests"
            //);

            // 503: الـ quota بتاعتنا خلصت.
            case TranslationOutcome.RateLimited rateLimited:
                if (rateLimited.RetryAfter is not null)
                    Response.Headers.RetryAfter = ((int)rateLimited.RetryAfter.Value.TotalSeconds).ToString();
                return StatusCode(
                    503,
                    ApiResponse<object>.Error(503, "The translation service is temporarily unavailable. Please try again later")
                );
                return Problem(
                    detail: "The translation service is temporarily unavailable. Please try again later",
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Service Unavailable"
                );

            // 500: Gemini رفض الطلب بتاعنا (body مش مظبوط أو key مرفوض).
            case TranslationOutcome.InvalidRequest:
                return StatusCode(
                    500,
                    ApiResponse<object>.Error(500, "The translation service rejected the request")
                );
                return Problem(
                    detail: "The translation service rejected the request",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error"
                );

            // 504:الـ timeout معناه "جرّب تاني ممكن ينفع"،
            case TranslationOutcome.UpstreamTimeout:
                return StatusCode(
                    504,
                    ApiResponse<object>.Error(504, "The translation service did not respond in time")
                );
                return Problem(
                    detail: "The translation service did not respond in time",
                    statusCode: StatusCodes.Status504GatewayTimeout,
                    title: "Gateway Timeout"
                );

            // 502: إحنا gateway قدام Gemini، وGemini مردش أو رد برد مش صالح.
            case TranslationOutcome.UpstreamError:
                return StatusCode(
                    502,
                    ApiResponse<object>.Error(502, "The translation service returned an invalid response")
                );
                return Problem(
                    detail: "The translation service returned an invalid response",
                    statusCode: StatusCodes.Status502BadGateway,
                    title: "Bad Gateway"
                );

            // 422: الطلب صحيح شكلاً ومفهوم، بس المحتوى نفسه اترفض.
            // دي الحالة الوحيدة هنا اللي العميل يقدر يتصرف فيها — يغيّر النص، فبنقوله السبب.
            // case TranslationOutcome.NotCompleted notCompleted
            //         when notCompleted.FinishReason is GeminiFinishReason.Safety or GeminiFinishReason.Recitation:
            //     return Problem(
            //         detail: $"The text could not be translated. Reason: {notCompleted.FinishReason}.",
            //         statusCode: StatusCodes.Status422UnprocessableEntity,
            //         title: "Unprocessable Entity"
            //     );

            // 504: الـ interaction لسه شغالة عند Gemini وإحنا مش بنعمل polling.
            //      "جرّب تاني" إجابة صادقة — على عكس 502 اللي معناها المزوّد بايظ.
            case TranslationOutcome.NotCompleted notCompleted
                    when notCompleted.Status is GeminiInteractionStatus.Queued or GeminiInteractionStatus.InProgress:
                return StatusCode(
                   504,
                   ApiResponse<object>.Error(504, "The translation service did not finish in time")
               );
                return Problem(
                    detail: "The translation service did not finish in time.",
                    statusCode: StatusCodes.Status504GatewayTimeout,
                    title: "Gateway Timeout"
                );

            // 500: الرد اتقطع (max tokens غالباً) — ده حد إعدادنا إحنا، مش غلطة العميل.
            case TranslationOutcome.NotCompleted notCompleted
                    when notCompleted.Status is GeminiInteractionStatus.Incomplete:
                return StatusCode(
                    500,
                    ApiResponse<object>.Error(500, "The translation could not be completed")
                );
                return Problem(
                    detail: "The translation could not be completed",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error"
                );

            // 502: failed / cancelled / unknown — المزوّد وقع أو رجّع حاجة مش فاهمينها.
            // ⚠️ دَين: الـ 422 بتاعت الحجب (SAFETY) اتشالت — مالهاش مصدر موثّق في الـ API
            //         الجديدة. الـ errors[] بتتسجّل خام لحد ما نشوف حالة حقيقية.
            case TranslationOutcome.NotCompleted:
                return StatusCode(
    502,
    ApiResponse<object>.Error(502, "The translation service returned an unexpected result")
);
                return Problem(
                    detail: "The translation service returned an unexpected result",
                    statusCode: StatusCodes.Status502BadGateway,
                    title: "Bad Gateway"
                );

            default:
                return StatusCode(
    500,
    ApiResponse<object>.Error(500, "Unhandling expeption during Translate")
);
        }
    }

    private static string SecondsUntil(DateTimeOffset instant) =>
        Math.Max(1, (int)Math.Ceiling((instant - DateTimeOffset.UtcNow).TotalSeconds))
        .ToString(CultureInfo.InvariantCulture);
}
