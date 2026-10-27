using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using TranslyAI.Api.AppSettings;
using TranslyAI.Api.Data;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Entities;
using TranslyAI.Api.Enums;
using TranslyAI.Api.Services.IServices;
using TranslyAI.Api.Services.Result;

namespace TranslyAI.Api.Services;

public class TranslationService(
    TranslyDbContext dbContext,
    IGeminiApiService geminiApiService,
    IQuotaService quotaService,
    IToneService toneService,
    ILogger<TranslationService> logger) : ITranslationService
{

    public async Task<TranslationResult> TranslateAsync(
        TranslationRequestDto request,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var tone = await toneService.ResolveAsync(request.Tone, cancellationToken);

        if (tone is null)
        {
            var snapshot = await quotaService.GetSnapshotAsync(userId, cancellationToken);
            return new TranslationResult(new TranslationOutcome.UnknownTone(request.Tone!), snapshot, null);
        }

        var reservation = await quotaService.TryReservationAsync(userId, request, tone.Id, cancellationToken);

        switch (reservation)
        {
            case QuotaReservation.Granted granted:
                return await TranslateReservedAsync(request, tone, granted, cancellationToken);

            case QuotaReservation.Exceeded exceeded:
                logger.LogInformation(
                    "Quota exceeded for {UserId} ({Used}/{Limit} characters) - Gemini was not called.",
                    userId, exceeded.Snapshot.Used, exceeded.Snapshot.Limit);
                return new TranslationResult(new TranslationOutcome.QuotaExceeded(), exceeded.Snapshot, tone);

            case QuotaReservation.TextTooLong tooLong:
                return new TranslationResult(new TranslationOutcome.TextTooLong(tooLong.MaxCharacters), tooLong.Snapshot, tone);

            default:
                throw new UnreachableException($"Unknown reservation type {reservation.GetType().Name}.");
        }
    }

    private async Task<TranslationResult> TranslateReservedAsync(
        TranslationRequestDto request,
        ResolvedTone tone,
        QuotaReservation.Granted reservation,
        CancellationToken cancellationToken)
    {
        TranslationOutcome outcome;
        TranslationSource source;

        try
        {
            (outcome, source) = await TranslateWithCacheAsync(request, tone, cancellationToken);
        }
        catch
        {
            await quotaService.ReleaseAsync(reservation.UsageId);
            throw;
        }

        if (outcome is not TranslationOutcome.Success)
        {
            await quotaService.ReleaseAsync(reservation.UsageId);
            return new TranslationResult(outcome, reservation.Before, tone);
        }

        await quotaService.CommitAsync(reservation.UsageId, source);
        return new TranslationResult(outcome, reservation.After, tone);
    }

    private async Task<(TranslationOutcome outcome, TranslationSource source)> TranslateWithCacheAsync(
        TranslationRequestDto request,
        ResolvedTone tone,
        CancellationToken cancellationToken)
    {
        var cacheKey = BuildCacheKey(request, tone);

        var model = geminiApiService.ModelName();

        var cached = await dbContext.CachedTranslations
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.CacheKey == cacheKey && t.Model == model, cancellationToken);

        if (cached is not null)
        {
            return (
                new TranslationOutcome.Success(
                    cached.TranslatedText,
                    cached.Model,
                    new DateTimeOffset(cached.CreatedAtUtc, TimeSpan.Zero)),
                TranslationSource.Cache);
        }

        var outcome = await geminiApiService.TranslateAsync(request, tone.PromptInstruction, cancellationToken);

        if (outcome is TranslationOutcome.Success success)
        {
            await CacheAsync(request, tone, cacheKey, success, cancellationToken);
        }

        return (outcome, TranslationSource.Gemini);
    }

    private async Task CacheAsync(
        TranslationRequestDto requestDto,
        ResolvedTone tone,
        string cacheKey,
        TranslationOutcome.Success success,
        CancellationToken cancellationToken)
    {
        var entry = new CachedTranslation
        {
            CacheKey = cacheKey,
            SourceLanguage = requestDto.SourceLanguage,
            TargetLanguage = requestDto.TargetLanguage,
            ToneId = tone.Id,
            PromptVersion = tone.PromptVersion,
            SourceText = requestDto.Text,
            TranslatedText = success.Text,
            Model = success.Model,
            CreatedAtUtc = success.CreatedAt.UtcDateTime,
        };

        dbContext.CachedTranslations.Add(entry);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            dbContext.Entry(entry).State = EntityState.Detached;
            logger.LogWarning(ex, "Could not cache the translation for {CacheKey}.", cacheKey);
        }
    }

    private static string BuildCacheKey(TranslationRequestDto request, ResolvedTone tone)
    {
        var material = $"{request.SourceLanguage}\n{request.TargetLanguage}\n{tone.Code}@{tone.PromptVersion}\n{request.Text}";

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
    }
}
