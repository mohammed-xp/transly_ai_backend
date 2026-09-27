namespace TranslyAI.Api.Services
{
    public abstract record QuotaReservation
    {
        private QuotaReservation() { }

        public sealed record Granted(
            long UsageId,
            QuotaSnapshot Before,
            QuotaSnapshot After
        ) : QuotaReservation;

        public sealed record Exceeded(
            QuotaSnapshot Snapshot
        ) : QuotaReservation;

        public sealed record TextTooLong(
            int MaxCharacters,
            QuotaSnapshot Snapshot
        ) : QuotaReservation;
    }
}
