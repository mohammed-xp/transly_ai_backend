using System.Diagnostics;
using TranslyAI.Api.Common;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Services.IServices;
using TranslyAI.Api.Services.Result;

namespace TranslyAI.Api.Services;

public class ConversationService(
    IGeminiApiService geminiApiService,
    IQuotaService quotaService,
    ILogger<ConversationService> logger) : IConversationService
{
    // أقل من كده غالباً ضغطة بالغلط على الـ mic
    public const double MinSpeechSeconds = 0.5;

    // الـ recorders بتزوّد أجزاء من الثانية — 30.03 ثانية تتحسب 30 مش 31
    public const double BillingToleranceSeconds = 0.1;

    public async Task<ConversationResult> TranslateTurnAsync(
        byte[] wavAudio,
        string languageA,
        string languageB,
        IReadOnlyList<ConversationHistoryItemDto> history,
        Guid userId,
        CancellationToken cancellationToken)
    {
        // 1) الملف نفسه — أرخص فحص، من غير DB ولا Gemini
        if (!WavInspector.TryRead(wavAudio, out var wav))
        {
            var snapshot = await quotaService.GetAudioSnapshotAsync(userId, cancellationToken);
            return new ConversationResult(
                new ConversationOutcome.InvalidAudio("The audio must be an uncompressed PCM WAV file."),
                snapshot,
                AudioSeconds: 0);
        }

        var audioSeconds = BillableSeconds(wav.DurationSeconds);

        // 2) تسجيل قصير جداً: أكيد مفيش كلام — ما بنكلمش Gemini ولا بنخصم
        if (wav.DurationSeconds < MinSpeechSeconds)
        {
            var snapshot = await quotaService.GetAudioSnapshotAsync(userId, cancellationToken);
            return new ConversationResult(
                new ConversationOutcome.NoSpeech(geminiApiService.ConversationModelName()),
                snapshot,
                audioSeconds);
        }

        // 3) حجز الثواني قبل ما نكلم Gemini
        var reservation = await quotaService.TryReserveAudioAsync(
            userId, languageA, languageB, audioSeconds, cancellationToken);

        switch (reservation)
        {
            case AudioQuotaReservation.Granted granted:
                return await TranslateReservedAsync(
                    wavAudio, languageA, languageB, history, audioSeconds, granted, cancellationToken);

            case AudioQuotaReservation.Exceeded exceeded:
                logger.LogInformation(
                    "Audio quota exceeded for {UserId} ({Used}/{Limit} seconds) - Gemini was not called.",
                    userId, exceeded.Snapshot.Used, exceeded.Snapshot.Limit);
                return new ConversationResult(new ConversationOutcome.QuotaExceeded(), exceeded.Snapshot, audioSeconds);

            case AudioQuotaReservation.AudioTooLong tooLong:
                return new ConversationResult(
                    new ConversationOutcome.AudioTooLong(tooLong.MaxSeconds), tooLong.Snapshot, audioSeconds);

            default:
                throw new UnreachableException($"Unknown reservation type {reservation.GetType().Name}.");
        }
    }

    private async Task<ConversationResult> TranslateReservedAsync(
        byte[] wavAudio,
        string languageA,
        string languageB,
        IReadOnlyList<ConversationHistoryItemDto> history,
        int audioSeconds,
        AudioQuotaReservation.Granted reservation,
        CancellationToken cancellationToken)
    {
        ConversationOutcome outcome;

        try
        {
            outcome = await geminiApiService.TranslateConversationTurnAsync(
                wavAudio, languageA, languageB, history, cancellationToken);
        }
        catch
        {
            await quotaService.ReleaseAudioAsync(reservation.UsageId);
            throw;
        }

        // بنخصم بس لو المستخدم خد ترجمة فعلاً
        if (outcome is not ConversationOutcome.Translated)
        {
            await quotaService.ReleaseAudioAsync(reservation.UsageId);
            return new ConversationResult(outcome, reservation.Before, audioSeconds);
        }

        await quotaService.CommitAudioAsync(reservation.UsageId);
        return new ConversationResult(outcome, reservation.After, audioSeconds);
    }

    private static int BillableSeconds(double durationSeconds) =>
        Math.Max(1, (int)Math.Ceiling(durationSeconds - BillingToleranceSeconds));
}