using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TranslyAI.Api.AppSettings;
using TranslyAI.Api.Data;
using TranslyAI.Api.Entities;
using TranslyAI.Api.Services.IServices;
using TranslyAI.Api.Services.Result;

namespace TranslyAI.Api.Tests.Integration;

public class AudioQuotaTests(TranslyApiFactory factory)
    : IClassFixture<TranslyApiFactory>, IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TranslyDbContext>();

        await db.ConversationUsages.ExecuteDeleteAsync();
        await db.TranslationUsages.ExecuteDeleteAsync();
        await db.Users.ExecuteDeleteAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task TryReserveAudio_WithinLimit_IsGrantedAndCountsWhileReserved()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var quota = scope.ServiceProvider.GetRequiredService<IQuotaService>();
        var userId = await CreateUserAsync(scope);

        var reservation = await quota.TryReserveAudioAsync(userId, "ar", "en", 10, CancellationToken.None);

        var granted = Assert.IsType<AudioQuotaReservation.Granted>(reservation);
        Assert.Equal(0, granted.Before.Used);
        Assert.Equal(10, granted.After.Used);

        // الحجز بيتحسب حتى قبل الـ commit — عشان الطلبات المتزامنة
        var snapshot = await quota.GetAudioSnapshotAsync(userId, CancellationToken.None);
        Assert.Equal(10, snapshot.Used);
    }

    [Fact]
    public async Task CommitAudio_KeepsTheSecondsCounted()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var quota = scope.ServiceProvider.GetRequiredService<IQuotaService>();
        var userId = await CreateUserAsync(scope);

        var granted = Assert.IsType<AudioQuotaReservation.Granted>(
            await quota.TryReserveAudioAsync(userId, "ar", "en", 12, CancellationToken.None));

        await quota.CommitAudioAsync(granted.UsageId);

        var snapshot = await quota.GetAudioSnapshotAsync(userId, CancellationToken.None);
        Assert.Equal(12, snapshot.Used);
    }

    [Fact]
    public async Task ReleaseAudio_GivesTheSecondsBack()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var quota = scope.ServiceProvider.GetRequiredService<IQuotaService>();
        var userId = await CreateUserAsync(scope);

        var granted = Assert.IsType<AudioQuotaReservation.Granted>(
            await quota.TryReserveAudioAsync(userId, "ar", "en", 12, CancellationToken.None));

        await quota.ReleaseAudioAsync(granted.UsageId);

        var snapshot = await quota.GetAudioSnapshotAsync(userId, CancellationToken.None);
        Assert.Equal(0, snapshot.Used);
    }

    [Fact]
    public async Task TryReserveAudio_LongerThanPlanAllows_ReturnsAudioTooLong()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var quota = scope.ServiceProvider.GetRequiredService<IQuotaService>();
        var limits = Limits(scope.ServiceProvider);
        var userId = await CreateUserAsync(scope);

        var reservation = await quota.TryReserveAudioAsync(
            userId, "ar", "en", limits.MaxAudioSecondsPerRequest + 1, CancellationToken.None);

        var tooLong = Assert.IsType<AudioQuotaReservation.AudioTooLong>(reservation);
        Assert.Equal(limits.MaxAudioSecondsPerRequest, tooLong.MaxSeconds);
    }

    [Fact]
    public async Task TryReserveAudio_WhenWindowIsFull_ReturnsExceeded()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var quota = scope.ServiceProvider.GetRequiredService<IQuotaService>();
        var limits = Limits(scope.ServiceProvider);
        var userId = await CreateUserAsync(scope);

        // نملى الـ window كله بتسجيلات بأقصى طول مسموح
        var remaining = limits.AudioSecondsPerWindow;
        while (remaining > 0)
        {
            var seconds = Math.Min(remaining, limits.MaxAudioSecondsPerRequest);
            var granted = Assert.IsType<AudioQuotaReservation.Granted>(
                await quota.TryReserveAudioAsync(userId, "ar", "en", seconds, CancellationToken.None));

            await quota.CommitAudioAsync(granted.UsageId);
            remaining -= seconds;
        }

        var reservation = await quota.TryReserveAudioAsync(userId, "ar", "en", 1, CancellationToken.None);

        var exceeded = Assert.IsType<AudioQuotaReservation.Exceeded>(reservation);
        Assert.Equal(limits.AudioSecondsPerWindow, exceeded.Snapshot.Used);
        Assert.Equal(0, exceeded.Snapshot.Remaining);
    }

    [Fact]
    public async Task AudioUsage_DoesNotTouchTheCharacterQuota()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var quota = scope.ServiceProvider.GetRequiredService<IQuotaService>();
        var userId = await CreateUserAsync(scope);

        var granted = Assert.IsType<AudioQuotaReservation.Granted>(
            await quota.TryReserveAudioAsync(userId, "ar", "en", 20, CancellationToken.None));
        await quota.CommitAudioAsync(granted.UsageId);

        var characters = await quota.GetSnapshotAsync(userId, CancellationToken.None);
        Assert.Equal(0, characters.Used);
    }

    private static async Task<Guid> CreateUserAsync(AsyncServiceScope scope)
    {
        var db = scope.ServiceProvider.GetRequiredService<TranslyDbContext>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid():N}@transly.test",
            UserName = "audio-tester",
            CreatedAtUtc = DateTime.UtcNow,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user.Id;
    }

    private static PlanLimits Limits(IServiceProvider services)
    {
        var plans = services.GetRequiredService<IOptions<PlansOptions>>().Value;
        return plans.Catalog[plans.DefaultPlan];
    }
}