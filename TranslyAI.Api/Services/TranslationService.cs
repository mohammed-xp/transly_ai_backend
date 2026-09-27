using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using TranslyAI.Api.AppSettings;
using TranslyAI.Api.Data;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Entities;
using TranslyAI.Api.Enums;
using TranslyAI.Api.Services.IServices;

namespace TranslyAI.Api.Services;

public class TranslationService(
    TranslyDbContext dbContext,
    IGeminiApiService geminiApiService,
    IQuotaService quotaService,
    ILogger<TranslationService> logger) : ITranslationService
{

    public async Task<TranslationResult> TranslateAsync(
        TranslationRequestDto request,
        Guid userId,
        CancellationToken cancellationToken)
    {

        var reservation = await quotaService.TryReservationAsync(userId, request, cancellationToken);

        switch (reservation)
        {
            case QuotaReservation.Granted granted:
                return await TranslateReservedAsync(request, granted, cancellationToken);

            case QuotaReservation.Exceeded exceeded:
                logger.LogInformation(
                    "Quota exceeded for {UserId} ({Used}/{Limit} characters) - Gemini was not called.",
                    userId, exceeded.Snapshot.Used, exceeded.Snapshot.Limit);
                return new TranslationResult(new TranslationOutcome.QuotaExceeded(), exceeded.Snapshot);

            case QuotaReservation.TextTooLong tooLong:
                return new TranslationResult(new TranslationOutcome.TextTooLong(tooLong.MaxCharacters), tooLong.Snapshot);

            default:
                throw new UnreachableException($"Unknown reservation type {reservation.GetType().Name}.");
        }
    }

    private async Task<TranslationResult> TranslateReservedAsync(
        TranslationRequestDto request,
        QuotaReservation.Granted reservation,
        CancellationToken cancellationToken)
    {
        TranslationOutcome outcome;
        TranslationSource source;

        try
        {
            (outcome, source) = await TranslateWithCacheAsync(request, cancellationToken);
        }
        catch
        {
            await quotaService.ReleaseAsync(reservation.UsageId);
            throw;
        }

        if(outcome is not TranslationOutcome.Success)
        {
            await quotaService.ReleaseAsync(reservation.UsageId);
            return new TranslationResult(outcome, reservation.Before);
        }

        await quotaService.CommitAsync(reservation.UsageId, source);
        return new TranslationResult(outcome, reservation.After);
    }

    private async Task<(TranslationOutcome outcome, TranslationSource source)> TranslateWithCacheAsync(
        TranslationRequestDto request,
        CancellationToken cancellationToken)
    {
        var cacheKey = BuildCacheKey(request);

        var model = geminiApiService.ModelName();

        var cached = await dbContext.CachedTranslations
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.CacheKey == cacheKey && t.Model == model, cancellationToken);

        if(cached is not null)
        {
            return (
                new TranslationOutcome.Success(
                    cached.TranslatedText,
                    cached.Model,
                    new DateTimeOffset(cached.CreatedAtUtc, TimeSpan.Zero)),
                TranslationSource.Cache);
        }

        var outcome = await geminiApiService.TranslateAsync(request, cancellationToken);

        if(outcome is TranslationOutcome.Success success)
        {
            await CacheAsync(request, cacheKey, success, cancellationToken);
        }

        return (outcome, TranslationSource.Gemini);
    }

    private async Task CacheAsync(
        TranslationRequestDto requestDto,
        string cacheKey,
        TranslationOutcome.Success success,
        CancellationToken cancellationToken)
    {
        var entry = new CachedTranslation
        {
            CacheKey = cacheKey,
            SourceLanguage = requestDto.SourceLanguage,
            TargetLanguage = requestDto.TargetLanguage,
            Tone = requestDto.Tone,
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
        catch(DbUpdateException ex)
        {
            dbContext.Entry(entry).State = EntityState.Detached;
            logger.LogWarning(ex, "Could not cache the translation for {CacheKey}.", cacheKey);
        }
    }

    private static string BuildCacheKey(TranslationRequestDto request)
    {
        var material = $"{request.SourceLanguage}\n{request.TargetLanguage}\n{request.Tone}\n{request.Text}";

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
    }
}
