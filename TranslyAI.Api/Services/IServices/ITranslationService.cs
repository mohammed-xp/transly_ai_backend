using TranslyAI.Api.Dtos;

namespace TranslyAI.Api.Services.IServices
{
    public interface ITranslationService
    {
        Task<TranslationResult> TranslateAsync(
        TranslationRequest request,
        Guid userId,
        CancellationToken cancellationToken);
    }
}
