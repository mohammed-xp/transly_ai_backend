using Microsoft.EntityFrameworkCore;
using TranslyAI.Api.Entities;

namespace TranslyAI.Api.Data;

public class TranslyDbContext(DbContextOptions<TranslyDbContext> options) : DbContext(options)
{
    public DbSet<CachedTranslation> CachedTranslations =>
        Set<CachedTranslation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CachedTranslation>(entity =>
        {
            entity.HasIndex(t => new { t.CacheKey, t.Model }).IsUnique();

            entity.Property(t => t.Tone).HasConversion<string>().HasMaxLength(16);

            entity.Property(t => t.CreatedAtUtc).HasColumnType("datetime(6)");
        });
    }
}