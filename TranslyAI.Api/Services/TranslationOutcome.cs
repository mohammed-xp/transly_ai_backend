using TranslyAI.Api.Enums;

namespace TranslyAI.Api.Services;

public abstract record TranslationOutcome
{
    private TranslationOutcome() { }

    public sealed record Success(
        string Text,
        string Model,
        DateTimeOffset CreatedAt
    ) : TranslationOutcome;

    public sealed record RateLimited(
        TimeSpan? RetryAfter
    ) : TranslationOutcome;

    public sealed record InvalidRequest() : TranslationOutcome;

    public sealed record UpstreamError() : TranslationOutcome;

    public sealed record UpstreamTimeout() : TranslationOutcome;

    public sealed record QuotaExceeded() : TranslationOutcome;

    public sealed record NotCompleted(
        GeminiInteractionStatus Status
    ) : TranslationOutcome;
}
