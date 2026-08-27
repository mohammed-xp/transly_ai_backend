using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TranslyAI.Api.Data;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Entities;

namespace TranslyAI.Api.Services;

public class TranslationService(
    TranslyDbContext dbContext,
    GeminiApiService geminiApiService,
    ILogger<TranslationService> logger)
{
    public async Task<TranslationOutcome> TranslateAsync(
        TranslationRequest request,
        CancellationToken cancellationToken)
    {
        var cacheKey = BuildCacheKey(request);

        var cached = await dbContext.CachedTranslations
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.CacheKey == cacheKey, cancellationToken);

        if (cached is not null)
        {
            logger.LogInformation("Cache HIT {CacheKey} - Gemini was not called.", cacheKey);

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
            logger.LogWarning(ex, "Could not cache the translationfor {CacheKey}.", cacheKey);
        }

        return success;
    }

    private static string BuildCacheKey(TranslationRequest request)
    {
        var material = $"{request.SourceLanguage}\n{request.TargetLanguage}\n{request.Tone}\n{request.Text}";

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
    }
}