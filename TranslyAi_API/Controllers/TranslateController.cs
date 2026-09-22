using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Transactions;
using TranslyAi_API.Common.Constants;
using TranslyAi_API.Dtos;
using TranslyAi_API.Extentions;
using TranslyAi_API.Services.IServices;

namespace TranslyAi_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TranslateController(ITranslationService translationService) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<TranslateResponseDto>> Translate(TranslateRequestDto translateRequestDto, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            if (userId is null)
            {
                return Unauthorized();
            }

            var request = translateRequestDto with
            {
                SourceLanguage = LanguageCatalog.Get(translateRequestDto.SourceLanguage).Code,
                TargetLanguage = LanguageCatalog.Get(translateRequestDto.TargetLanguage).Code,
            };
            var result = await translationService.TranslateAsync(request, cancellationToken);

            //Response.Headers["X-RateLimit-Limit"] = result.Quota.Limit.ToString(CultureInfo.InvariantCulture);
            //Response.Headers["X-RateLimit-Remaining"] = result.Quota.Remaining.ToString(CultureInfo.InvariantCulture);
            //Response.Headers["X-RateLimit-Reset"] = result.Quota.ResetsAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

            switch (result.Outcome)
            {
                case TranslationOutcome.Success success:
                    return Ok(new TranslationResponse
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
                    return Problem(
                        detail: "Youhave used your daily translation quota.",
                        statusCode: StatusCodes.Status429TooManyRequests,
                        title: "Too Many Requests"
                    );

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
                    return Problem(
                        detail: "The translation service did not finish in time.",
                        statusCode: StatusCodes.Status504GatewayTimeout,
                        title: "Gateway Timeout"
                    );

                // 500: الرد اتقطع (max tokens غالباً) — ده حد إعدادنا إحنا، مش غلطة العميل.
                case TranslationOutcome.NotCompleted notCompleted
                        when notCompleted.Status is GeminiInteractionStatus.Incomplete:
                    return Problem(
                        detail: "The translation could not be completed.",
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Internal Server Error"
                    );

                // 502: failed / cancelled / unknown — المزوّد وقع أو رجّع حاجة مش فاهمينها.
                // ⚠️ دَين: الـ 422 بتاعت الحجب (SAFETY) اتشالت — مالهاش مصدر موثّق في الـ API
                //         الجديدة. الـ errors[] بتتسجّل خام لحد ما نشوف حالة حقيقية.
                case TranslationOutcome.NotCompleted:
                    return Problem(
                        detail: "The translation service returned an unexpected result.",
                        statusCode: StatusCodes.Status502BadGateway,
                        title: "Bad Gateway"
                    );

                default:
                    throw new ArgumentOutOfRangeException(
                        "Unhandled translation outcome.");
            }
        }
    }
}
