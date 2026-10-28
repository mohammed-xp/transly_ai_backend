using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using TranslyAI.Api.Enums;
using TranslyAI.Api.Services.Result;

namespace TranslyAI.Api.Extensions;

public static class UpstreamProblemExtensions
{
    // بيحوّل أخطاء Gemini لنفس الـ status codes في كل الـ endpoints.
    // بيرجّع null لو الـ outcome مش خطأ upstream — والـ caller يقرر يعمل إيه.
    public static ObjectResult? UpstreamProblem(this ControllerBase controller, TranslationOutcome outcome)
    {
        switch (outcome)
        {
            // 503: الـ quota بتاعتنا خلصت.
            case TranslationOutcome.RateLimited rateLimited:
                if (rateLimited.RetryAfter is not null)
                    controller.Response.Headers.RetryAfter =
                        ((int)rateLimited.RetryAfter.Value.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                return controller.ServiceUnavailableProblem("The translation service is temporarily unavailable. Please try again later");

            // 500: Gemini رفض الطلب بتاعنا (body مش مظبوط أو key مرفوض).
            case TranslationOutcome.InvalidRequest:
                return controller.InternalErrorProblem("The translation service rejected the request");

            // 504:الـ timeout معناه "جرّب تاني ممكن ينفع"،
            case TranslationOutcome.UpstreamTimeout:
                return controller.GatewayTimeoutProblem("The translation service did not respond in time");

            // 502: إحنا gateway قدام Gemini، وGemini مردش أو رد برد مش صالح.
            case TranslationOutcome.UpstreamError:
                return controller.BadGatewayProblem("The translation service returned an invalid response");

            // 504: الـ interaction لسه شغالة عند Gemini وإحنا مش بنعمل polling.
            //      "جرّب تاني" إجابة صادقة — على عكس 502 اللي معناها المزوّد بايظ.
            case TranslationOutcome.NotCompleted notCompleted
                when notCompleted.Status is GeminiInteractionStatus.Queued or GeminiInteractionStatus.InProgress:
                return controller.GatewayTimeoutProblem("The translation service did not finish in time");

            // 500: الرد اتقطع (max tokens غالباً) — ده حد إعدادنا إحنا، مش غلطة العميل.
            case TranslationOutcome.NotCompleted notCompleted
                when notCompleted.Status is GeminiInteractionStatus.Incomplete:
                return controller.InternalErrorProblem("The translation could not be completed");

            // 502: failed / cancelled / unknown — المزوّد وقع أو رجّع حاجة مش فاهمينها.
            // ⚠️ دَين: الـ 422 بتاعت الحجب (SAFETY) اتشالت — مالهاش مصدر موثّق في الـ API
            //         الجديدة. الـ errors[] بتتسجّل خام لحد ما نشوف حالة حقيقية.
            case TranslationOutcome.NotCompleted:
                return controller.BadGatewayProblem("The translation service returned an unexpected result");

            default:
                return null;
        }
    }
}