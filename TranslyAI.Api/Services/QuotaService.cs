using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data;
using TranslyAI.Api.AppSettings;
using TranslyAI.Api.Data;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Entities;
using TranslyAI.Api.Enums;
using TranslyAI.Api.Services;
using TranslyAI.Api.Services.IServices;

namespace TranslyAI.Api.Services
{
    public class QuotaService(TranslyDbContext dbContext, IOptions<PlansOptions> plansOptions) : IQuotaService
    {

        public static readonly TimeSpan ReservationTimeout = TimeSpan.FromMinutes(2);
        public static int CountCharacters(string text) => text.EnumerateRunes().Count();

        public async Task CommitAsync(long usageId, TranslationSource source)
        {
            var usage = await dbContext.TranslationUsages.FindAsync(usageId)
                ?? throw new InvalidOperationException();

            usage.Status = UsageStatus.Committed;
            usage.Source = source;

            await dbContext.SaveChangesAsync();
        }

        public async Task<QuotaSnapshot> GetSnapshotAsync(Guid userId, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var period = CurrentPeriod(now);
            var used = await SumUsedAsync(userId, period.StartUtc, now, cancellationToken);

            return period.Snapshot(used);
        }

        public async Task ReleaseAsync(long UsageId)
        {
            var usage = await dbContext.TranslationUsages.FindAsync(UsageId);

            if (usage is null) return;

            dbContext.TranslationUsages.Remove(usage);

            await dbContext.SaveChangesAsync();
        }

        public async Task<QuotaReservation> TryReservationAsync(Guid userId, TranslationRequestDto request, CancellationToken cancellationToken)
        {
            var characters = CountCharacters(request.Text);
            var now = DateTime.UtcNow;
            QuotaPeriod period = CurrentPeriod(now);

            if(characters > period.Limits.MaxCharactersPerRequest)
            {
                var usedSoFar = await SumUsedAsync(userId, period.StartUtc, now, cancellationToken);

                return new QuotaReservation.TextTooLong(period.Limits.MaxCharactersPerRequest, period.Snapshot(usedSoFar));
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

            await dbContext.Users
                .FromSql($"SELECT * FROM Users WHERE Id = {userId} FOR UPDATE")
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var used = await SumUsedAsync(userId, period.StartUtc, now, cancellationToken);

            if(used+characters > period.Limits.CharactersPerWindow)
            {
                return new QuotaReservation.Exceeded(period.Snapshot(used));
            }

            var usage = new TranslationUsage
            {
                UserId = userId,
                SourceLanguage = request.SourceLanguage,
                TargetLanguage = request.TargetLanguage,
                Tone = request.Tone,
                CharacterCount = characters,
                Status = UsageStatus.Reserved,
                Source = null,
                CreatedAtUtc = now
            };

            dbContext.TranslationUsages.Add(usage);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new QuotaReservation.Granted(usage.Id, period.Snapshot(used), period.Snapshot(used + characters));
        }

        private Task<int> SumUsedAsync(Guid userId, DateTime windowStartUtc, DateTime now, CancellationToken cancellationToken)
        {
            var reservationCutoff = now - ReservationTimeout;

            return dbContext.TranslationUsages
                .Where(u => u.UserId == userId
                    && u.CreatedAtUtc >= windowStartUtc
                    && (u.Status == UsageStatus.Committed || u.CreatedAtUtc >= reservationCutoff))
                .SumAsync(u => u.CharacterCount, cancellationToken);
        }

        private QuotaPeriod CurrentPeriod(DateTime now)
        {
            var plans = plansOptions.Value;
            var limits = plans.Catalog[plans.DefaultPlan];
            var startUtc = now.Date;

            return new QuotaPeriod(
                plans.DefaultPlan,
                limits,
                startUtc,
                new DateTimeOffset(startUtc.AddDays(1), TimeSpan.Zero));
        }

        private sealed record QuotaPeriod(string Plan, PlanLimits Limits, DateTime StartUtc, DateTimeOffset ResetsAt)
        {
            public QuotaSnapshot Snapshot(int used) => new(Plan, Limits.CharactersPerWindow, used, ResetsAt);
        }

    }
}
