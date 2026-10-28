using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data;
using TranslyAI.Api.AppSettings;
using TranslyAI.Api.Data;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Entities;
using TranslyAI.Api.Enums;
using TranslyAI.Api.Services.IServices;
using TranslyAI.Api.Services.Result;

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

        public async Task<QuotaReservation> TryReservationAsync(Guid userId, TranslationRequestDto request, int toneId, CancellationToken cancellationToken)
        {
            var characters = CountCharacters(request.Text);
            var now = DateTime.UtcNow;
            QuotaPeriod period = CurrentPeriod(now);

            if (characters > period.Limits.MaxCharactersPerRequest)
            {
                var usedSoFar = await SumUsedAsync(userId, period.StartUtc, now, cancellationToken);

                return new QuotaReservation.TextTooLong(period.Limits.MaxCharactersPerRequest, period.Snapshot(usedSoFar));
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

            await LockUserRowAsync(userId, cancellationToken);

            var used = await SumUsedAsync(userId, period.StartUtc, now, cancellationToken);

            if (used + characters > period.Limits.CharactersPerWindow)
            {
                return new QuotaReservation.Exceeded(period.Snapshot(used));
            }

            var usage = new TranslationUsage
            {
                UserId = userId,
                SourceLanguage = request.SourceLanguage,
                TargetLanguage = request.TargetLanguage,
                ToneId = toneId,
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

        public async Task<QuotaSnapshot> GetAudioSnapshotAsync(Guid userId, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var period = CurrentPeriod(now);
            var used = await SumAudioSecondsUsedAsync(userId, period.StartUtc, now, cancellationToken);

            return period.AudioSnapshot(used);
        }

        public async Task<AudioQuotaReservation> TryReserveAudioAsync(Guid userId, string languageA, string languageB, int audioSeconds, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var period = CurrentPeriod(now);

            // فحص الطول الأول — من غير transaction ولا lock
            if (audioSeconds > period.Limits.MaxAudioSecondsPerRequest)
            {
                var usedSoFar = await SumAudioSecondsUsedAsync(userId, period.StartUtc, now, cancellationToken);

                return new AudioQuotaReservation.AudioTooLong(
                    period.Limits.MaxAudioSecondsPerRequest,
                    period.AudioSnapshot(usedSoFar));
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

            await LockUserRowAsync(userId, cancellationToken);

            var used = await SumAudioSecondsUsedAsync(userId, period.StartUtc, now, cancellationToken);

            if (used + audioSeconds > period.Limits.AudioSecondsPerWindow)
            {
                // الـ transaction بتعمل rollback لوحدها مع الـ dispose
                return new AudioQuotaReservation.Exceeded(period.AudioSnapshot(used));
            }

            var usage = new ConversationUsage
            {
                UserId = userId,
                LanguageA = languageA,
                LanguageB = languageB,
                AudioSeconds = audioSeconds,
                Status = UsageStatus.Reserved,
                CreatedAtUtc = now
            };

            dbContext.ConversationUsages.Add(usage);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new AudioQuotaReservation.Granted(
                usage.Id,
                period.AudioSnapshot(used),
                period.AudioSnapshot(used + audioSeconds));
        }

        public async Task CommitAudioAsync(long usageId)
        {
            var usage = await dbContext.ConversationUsages.FindAsync(usageId)
                ?? throw new InvalidOperationException($"Conversation usage {usageId} was not found.");
            
            usage.Status = UsageStatus.Committed;
            await dbContext.SaveChangesAsync();
        }

        public async Task ReleaseAudioAsync(long usageId)
        {
            var usage = await dbContext.ConversationUsages.FindAsync(usageId);
            
            if (usage is null) return;

            dbContext.ConversationUsages.Remove(usage);

            await dbContext.SaveChangesAsync();
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
            
            public QuotaSnapshot AudioSnapshot(int usedSeconds) => new(Plan, Limits.AudioSecondsPerWindow, usedSeconds, ResetsAt);
        }


        private Task<int> SumAudioSecondsUsedAsync(Guid userId, DateTime windowStartUtc, DateTime now, CancellationToken cancellationToken)
        {
            var reservationCutoff = now - ReservationTimeout;
            
            return dbContext.ConversationUsages
                .Where(u => u.UserId == userId
                    && u.CreatedAtUtc >= windowStartUtc
                    && (u.Status == UsageStatus.Committed || u.CreatedAtUtc >= reservationCutoff))
                .SumAsync(u => u.AudioSeconds, cancellationToken);
        }

        // بيقفل صف المستخدم لحد آخر الـ transaction —
        // أي reservation تانية لنفس المستخدم (نص أو صوت) بتستنى لحد ما دي تخلص
        private Task LockUserRowAsync(Guid userId, CancellationToken cancellationToken) =>
            dbContext.Users
                .FromSql($"SELECT * FROM Users WHERE Id = {userId} FOR UPDATE")
                .AsNoTracking()
                .ToListAsync(cancellationToken);
    }
}
