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
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<ApiResponse<TranslationResponseDto>>> Translate(
        TranslationRequestDto translation,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return this.UnauthorizedProblem("The access token is invalid");
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
                var response = ApiResponse<TranslationResponseDto>.Ok(new TranslationResponseDto
                {
                    SourceText = request.Text,
                    TranslatedText = success.Text,
                    SourceLanguage = request.SourceLanguage,
                    TargetLanguage = request.TargetLanguage,
                    Model = success.Model,
                    Tone = request.Tone,
                    CreatedAt = success.CreatedAt,
                }, "Translate successfully");
                return Ok(response);
            // 429: الـ quota الخاصة بالمستخدم خلصت.
            case TranslationOutcome.QuotaExceeded:
                Response.Headers.RetryAfter = SecondsUntil(result.Quota.ResetsAt);
                return this.TooManyRequestsProblem("You have used your daily translation quota");

            // 503: الـ quota بتاعتنا خلصت.
            case TranslationOutcome.RateLimited rateLimited:
                if (rateLimited.RetryAfter is not null)
                    Response.Headers.RetryAfter = ((int)rateLimited.RetryAfter.Value.TotalSeconds).ToString();
                return this.ServiceUnavailableProblem("The translation service is temporarily unavailable. Please try again later");

            // 500: Gemini رفض الطلب بتاعنا (body مش مظبوط أو key مرفوض).
            case TranslationOutcome.InvalidRequest:
                return this.InternalErrorProblem("The translation service rejected the request");

            // 504:الـ timeout معناه "جرّب تاني ممكن ينفع"،
            case TranslationOutcome.UpstreamTimeout:
                return this.GatewayTimeoutProblem("The translation service did not respond in time");

            // 502: إحنا gateway قدام Gemini، وGemini مردش أو رد برد مش صالح.
            case TranslationOutcome.UpstreamError:
                return this.BadGatewayProblem("The translation service returned an invalid response");

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
                return this.GatewayTimeoutProblem("The translation service did not finish in time");


            // 500: الرد اتقطع (max tokens غالباً) — ده حد إعدادنا إحنا، مش غلطة العميل.
            case TranslationOutcome.NotCompleted notCompleted
                    when notCompleted.Status is GeminiInteractionStatus.Incomplete:
                return this.InternalErrorProblem("The translation could not be completed");

            // 502: failed / cancelled / unknown — المزوّد وقع أو رجّع حاجة مش فاهمينها.
            // ⚠️ دَين: الـ 422 بتاعت الحجب (SAFETY) اتشالت — مالهاش مصدر موثّق في الـ API
            //         الجديدة. الـ errors[] بتتسجّل خام لحد ما نشوف حالة حقيقية.
            case TranslationOutcome.NotCompleted:
                return this.BadGatewayProblem("The translation service returned an unexpected result");

            default:
                return this.InternalErrorProblem("Unhandling expeption during Translate");
        }
    }

    private static string SecondsUntil(DateTimeOffset instant) =>
        Math.Max(1, (int)Math.Ceiling((instant - DateTimeOffset.UtcNow).TotalSeconds))
        .ToString(CultureInfo.InvariantCulture);
}
