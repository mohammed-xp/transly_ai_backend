using TranslyAI.Api.Dtos;
using TranslyAI.Api.Enums;
using TranslyAI.Api.Services.Result;

namespace TranslyAI.Api.Services.IServices
{
    public interface IQuotaService
    {
        Task<QuotaSnapshot> GetSnapshotAsync(Guid userId, CancellationToken cancellationToken);

        Task<QuotaReservation> TryReservationAsync(
            Guid userId,
            TranslationRequestDto request,
            int toneId,
            CancellationToken cancellationToken);

        Task CommitAsync(long usageId, TranslationSource source);

        Task ReleaseAsync(long UsageId);
    }
}
