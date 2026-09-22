using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
    IOptions<QuotaOptions> quotaOptions,
    ILogger<TranslationService> logger) : ITranslationService
{
    private readonly int _requestsPerDay = quotaOptions.Value.RequestsPerDay;

    public async Task<TranslationResult> TranslateAsync(
        TranslationRequest request,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var windowStart = DateTime.UtcNow.Date;
        var resetsAt = new DateTimeOffset(windowStart.AddDays(1), TimeSpan.Zero);

        var usedCount = await dbContext.TranslationUsages
            .CountAsync(
                usage => usage.UserId == userId && usage.CreatedAtUtc >= windowStart,
                cancellationToken);

        if (usedCount >= _requestsPerDay)
        {
            logger.LogInformation(
                "Quota exceeded for {UserId} ({UsedCount}/{Limit}) - Gemini was not called.",
                userId, usedCount, _requestsPerDay);

            return new TranslationResult(
                new TranslationOutcome.QuotaExceeded(),
                new QuotaSnapshot(_requestsPerDay, usedCount, resetsAt)
            );
        }

        var outcome = await TranslateWithinQuotaAsync(request, userId, cancellationToken);

        var usedCountConsumed = outcome is TranslationOutcome.Success ? usedCount + 1 : usedCount;

        return new TranslationResult(outcome, new QuotaSnapshot(_requestsPerDay, usedCountConsumed, resetsAt));

    }

    private static string BuildCacheKey(TranslationRequest request)
    {
        var material = $"{request.SourceLanguage}\n{request.TargetLanguage}\n{request.Tone}\n{request.Text}";

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
    }

    private async Task RecordUsageAsync(
        TranslationRequest request,
        Guid userId,
        TranslationSource source,
        CancellationToken cancellationToken)
    {
        dbContext.TranslationUsages.Add(new TranslationUsage
        {
            UserId = userId,
            SourceLanguage = request.SourceLanguage,
            TargetLanguage = request.TargetLanguage,
            Tone = request.Tone,
            CharacterCount = CountCharacters(request.Text),
            Source = source,
            CreatedAtUtc = DateTime.UtcNow,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static int CountCharacters(string text) => text.EnumerateRunes().Count();

    private async Task<TranslationOutcome> TranslateWithinQuotaAsync(
        TranslationRequest request,
        Guid userId,
        CancellationToken cancellationToken)
    {

        var cacheKey = BuildCacheKey(request);

        var model = geminiApiService.ModelName();

        var cached = await dbContext.CachedTranslations
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.CacheKey == cacheKey && t.Model == model, cancellationToken);

        if (cached is not null)
        {
            logger.LogInformation("Cache HIT {CacheKey} - Gemini was not called.", cacheKey);

            await RecordUsageAsync(
                request,
                userId,
                TranslationSource.Cache,
                cancellationToken
            );

            return new TranslationOutcome.Success(
                cached.TranslatedText,
                cached.Model,
                new DateTimeOffset(cached.CreatedAtUtc, TimeSpan.Zero)
            );
        }

        logger.LogInformation("Cache MISS {CacheKey} - calling Gemini.", cacheKey);

        var outcome = await geminiApiService.TranslateAsync(request, cancellationToken);

        if (outcome is not TranslationOutcome.Success success)
        {
            return outcome;
        }

        await RecordUsageAsync(
            request,
            userId,
            TranslationSource.Gemini,
            cancellationToken
        );

        dbContext.CachedTranslations.Add(new CachedTranslation
        {
            CacheKey = cacheKey,
            SourceLanguage = request.SourceLanguage,
            TargetLanguage = request.TargetLanguage,
            Tone = request.Tone,
            SourceText = request.Text,
            TranslatedText = success.Text,
            Model = success.Model,
            CreatedAtUtc = success.CreatedAt.UtcDateTime
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Could not cache the translation for {CacheKey}.", cacheKey);
        }

        return success;
    }
}