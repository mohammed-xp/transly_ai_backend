using TranslyAI.Api.Dtos;

namespace TranslyAI.Api.Services.IServices
{
    public interface IGeminiApiService
    {
        Task<TranslationOutcome> TranslateAsync(TranslationRequest translationRequest, CancellationToken cancellationToken);

        string ModelName();
    }
}
