using TranslyAI.Api.Dtos;
using TranslyAI.Api.Services.Result;

namespace TranslyAI.Api.Services.IServices
{
    public interface IGeminiApiService
    {
        Task<TranslationOutcome> TranslateAsync(
            TranslationRequestDto translationRequest,
            string toneInstruction,
            CancellationToken cancellationToken);
        
        Task<ConversationOutcome> TranslateConversationTurnAsync(
            byte[] wavAudio,
            string languageA,
            string languageB,
            IReadOnlyList<ConversationHistoryItemDto> history,
            CancellationToken cancellationToken);

        string ModelName();
        
        string ConversationModelName();
    }
}
