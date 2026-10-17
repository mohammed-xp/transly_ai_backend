using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using TranslyAI.Api.Data;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Services.IServices;
using TranslyAI.Api.Services.Result;

namespace TranslyAI.Api.Services;

public class ToneService(TranslyDbContext dbContext, IMapper mapper) : IToneService
{
    public async Task<ResolvedTone?> ResolveAsync(string? code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return await dbContext.Tones
                .Where(t => t.IsDefault == true)
                .ProjectTo<ResolvedTone>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("No default tone is configured.");
        }

        var normalized = code.Trim().ToLowerInvariant();

        return await dbContext.Tones
            .Where(t => t.Code == normalized)
            .ProjectTo<ResolvedTone>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ToneListResponseDto> GetListedAsync(CancellationToken cancellationToken)
    {
        var tones = await dbContext.Tones
            .Where(t => t.IsListed)
            .OrderBy(t => t.SortOrder)
            .ProjectTo<ToneDto>(mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        var defaultTone = await dbContext.Tones
            .Where(t => t.IsDefault == true)
            .Select(t => t.Code)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No default tone is configured.");

        return new ToneListResponseDto
        {
            DefaultTone = defaultTone,
            Tones = tones,
        };
    }
}
