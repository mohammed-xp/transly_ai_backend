using TranslyAI.Api.Dtos;
using TranslyAI.Api.Services.Result;

namespace TranslyAI.Api.Services.IServices;

public interface IConversationService
{
    Task<ConversationResult> TranslateTurnAsync(
        byte[] wavAudio,
        string languageA,
        string languageB,
        IReadOnlyList<ConversationHistoryItemDto> history,
        Guid userId,
        CancellationToken cancellationToken);
}