using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TranslyAI.Api.Common;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Enums;
using TranslyAI.Api.Extensions;
using TranslyAI.Api.Services.IServices;
using TranslyAI.Api.Services.Result;

namespace TranslyAI.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ConversationsController(IConversationService conversationService) : ControllerBase
{
    // 2MB للصوت + مساحة للـ history وحدود الـ multipart
    private const long MaxRequestBytes = 3 * 1024 * 1024;

    [HttpPost("turns")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    [ProducesResponseType<ApiResponse<ConversationTurnResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<ApiResponse<ConversationTurnResponseDto>>> TranslateTurn(
        [FromForm] ConversationTurnRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return this.UnauthorizedProblem("The access token is invalid");
        }

        // الـ [ApiController] عمل الـ validation خلاص: الملف موجود، والحجم مظبوط، واللغات مدعومة ومختلفة
        var languageA = LanguageCatalog.Get(request.LanguageA!).Code;
        var languageB = LanguageCatalog.Get(request.LanguageB!).Code;
        var audio = await request.Audio!.ReadAllBytesAsync(cancellationToken);

        var result = await conversationService.TranslateTurnAsync(
            audio, languageA, languageB, request.History, userId.Value, cancellationToken);

        SetAudioQuotaHeaders(result.Quota);

        switch (result.Outcome)
        {
            // 200: اتفهم الكلام واترجم
            case ConversationOutcome.Translated translated:
                return Ok(ApiResponse<ConversationTurnResponseDto>.Ok(new ConversationTurnResponseDto
                {
                    Status = ConversationTurnStatus.Ok,
                    SourceLanguage = LanguageCatalog.Get(translated.SourceLanguage),
                    TargetLanguage = LanguageCatalog.Get(translated.TargetLanguage),
                    Transcript = translated.Transcript,
                    Translation = translated.Translation,
                    AudioSeconds = result.AudioSeconds,
                    Model = translated.Model,
                    CreatedAt = translated.CreatedAt,
                }, "Turn translated successfully"));

            // 200: الطلب سليم بس مفيش كلام — نتيجة عادية مش error، ومفيش خصم
            case ConversationOutcome.NoSpeech noSpeech:
                return Ok(NotTranslated(
                    ConversationTurnStatus.NoSpeech, noSpeech.Model, result.AudioSeconds,
                    "No clear speech was detected"));

            // 200: الكلام بلغة غير اللغتين المختارين — ومفيش خصم
            case ConversationOutcome.UnsupportedLanguage unsupported:
                return Ok(NotTranslated(
                    ConversationTurnStatus.UnsupportedLanguage, unsupported.Model, result.AudioSeconds,
                    "The speech is not in either of the selected languages"));

            // 400: الملف مش WAV سليم — نفس شكل باقي الـ validation errors
            case ConversationOutcome.InvalidAudio invalidAudio:
                // ModelState.AddModelError("audio", invalidAudio.Reason);
                ModelState.AddModelError(nameof(ConversationTurnRequestDto.Audio), invalidAudio.Reason);
                return ValidationProblem(ModelState);

            // 400: التسجيل أطول من المسموح في الـ plan
            case ConversationOutcome.AudioTooLong tooLong:
                return this.BadRequestProblem(
                    $"The recording exceeds the maximum of {tooLong.MaxSeconds} seconds per turn for your plan");

            // 429: ثواني المستخدم خلصت
            case ConversationOutcome.QuotaExceeded:
                Response.Headers.RetryAfter = SecondsUntil(result.Quota.ResetsAt);
                return this.TooManyRequestsProblem(
                    $"Not enough conversation seconds left in your quota for this recording. Remaining: {result.Quota.Remaining}");

            // 503 / 504 / 502 / 500: نفس الـ mapping بتاع الترجمة العادية
            case ConversationOutcome.UpstreamFailure failure:
                return this.UpstreamProblem(failure.Error)
                    ?? this.InternalErrorProblem("Unhandled upstream error during conversation turn");

            default:
                return this.InternalErrorProblem("Unhandled outcome during conversation turn");
        }
    }

    private void SetAudioQuotaHeaders(QuotaSnapshot quota)
    {
        Response.Headers["X-Audio-Seconds-Limit"] = quota.Limit.ToString(CultureInfo.InvariantCulture);
        Response.Headers["X-Audio-Seconds-Remaining"] = quota.Remaining.ToString(CultureInfo.InvariantCulture);
        Response.Headers["X-Audio-Seconds-Reset"] = quota.ResetsAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
    }

    private static ApiResponse<ConversationTurnResponseDto> NotTranslated(
        ConversationTurnStatus status,
        string model,
        int audioSeconds,
        string message) =>
        ApiResponse<ConversationTurnResponseDto>.Ok(new ConversationTurnResponseDto
        {
            Status = status,
            AudioSeconds = audioSeconds,
            Model = model,
            CreatedAt = DateTimeOffset.UtcNow,
        }, message);

    private static string SecondsUntil(DateTimeOffset instant) =>
        Math.Max(1, (int)Math.Ceiling((instant - DateTimeOffset.UtcNow).TotalSeconds))
        .ToString(CultureInfo.InvariantCulture);
}