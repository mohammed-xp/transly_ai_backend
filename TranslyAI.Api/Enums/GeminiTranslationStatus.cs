namespace TranslyAI.Api.Enums;

public enum GeminiTranslationStatus
{
    Success,
    RateLimited,
    InvalidRequest,
    UpstreamError,
    BlockedBySafety,
    IncompleteResponse
}