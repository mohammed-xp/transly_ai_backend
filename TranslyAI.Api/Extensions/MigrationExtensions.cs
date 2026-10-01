using Microsoft.EntityFrameworkCore;
using TranslyAI.Api.Data;

namespace TranslyAI.Api.Extensions;

public static class MigrationExtensions
{
    /// <summary>
    /// بيطبّق أي migrations ناقصة على الداتابيز أول ما الـ app يبدأ.
    /// لو فشلت، الـ app ما بيكملش
    /// </summary>
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TranslyDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseMigration");

        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();

        if (pending.Count == 0)
        {
            logger.LogInformation("Database is up to date. No pending migrations.");
            return;
        }

        logger.LogInformation(
            "Applying {Count} pending migration(s): {Migrations}",
            pending.Count,
            string.Join(", ", pending));

        try
        {
            await db.Database.MigrateAsync();
            logger.LogInformation("Migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database migration failed. The app will not start.");
            throw;
        }
    }
}