namespace TranslyAI.Api.Services;

public sealed record QuotaSnapshot(int Limit, int Used, DateTimeOffset ResetsAt)
{
    public int Remaining => Math.Max(0, Limit - Used);
}

public sealed record TranslationResult(TranslationOutcome Outcome, QuotaSnapshot Quota);