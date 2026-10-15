using TranslyAI.Api.Dtos;
using TranslyAI.Api.Services.Result;

namespace TranslyAI.Api.Services.IServices
{
    public interface IGeminiApiService
    {
        Task<TranslationOutcome> TranslateAsync(TranslationRequestDto translationRequest, CancellationToken cancellationToken);

        string ModelName();
    }
}
