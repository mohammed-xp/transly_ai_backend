namespace TranslyAI.Api.Services.Result;

public abstract record AudioQuotaReservation
{
    private AudioQuotaReservation() { }

    // اتحجز — UsageId هنعمل بيه Commit أو Release بعد ما Gemini يرد
    public sealed record Granted(
        long UsageId,
        QuotaSnapshot Before,
        QuotaSnapshot After
    ) : AudioQuotaReservation;

    // الثواني المتبقية مش كفاية للتسجيل ده
    public sealed record Exceeded(
        QuotaSnapshot Snapshot
    ) : AudioQuotaReservation;

    // التسجيل الواحد أطول من المسموح في الـ plan
    public sealed record AudioTooLong(
        int MaxSeconds,
        QuotaSnapshot Snapshot
    ) : AudioQuotaReservation;
}