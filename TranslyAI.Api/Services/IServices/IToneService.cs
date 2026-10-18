using TranslyAI.Api.Dtos;
using TranslyAI.Api.Services.Result;

namespace TranslyAI.Api.Services.IServices;

public interface IToneService
{
    Task<ResolvedTone?> ResolveAsync(string? code, CancellationToken cancellationToken);

    Task<ToneListResponseDto> GetListedAsync(string culture, CancellationToken cancellationToken);
}
