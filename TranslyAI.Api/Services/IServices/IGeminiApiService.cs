using TranslyAI.Api.Dtos;

namespace TranslyAI.Api.Services.IServices
{
    public interface IGeminiApiService
    {
        Task<TranslationOutcome> TranslateAsync(TranslationRequestDto translationRequest, CancellationToken cancellationToken);

        string ModelName();
    }
}
