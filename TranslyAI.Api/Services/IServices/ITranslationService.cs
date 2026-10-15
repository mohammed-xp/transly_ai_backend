using TranslyAI.Api.Dtos;
using TranslyAI.Api.Services.Result;

namespace TranslyAI.Api.Services.IServices
{
    public interface ITranslationService
    {
        Task<TranslationResult> TranslateAsync(
        TranslationRequestDto request,
        Guid userId,
        CancellationToken cancellationToken);
    }
}
