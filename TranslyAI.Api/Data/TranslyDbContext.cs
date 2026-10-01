using Microsoft.EntityFrameworkCore;
using TranslyAI.Api.Entities;

namespace TranslyAI.Api.Data;

public class TranslyDbContext(DbContextOptions<TranslyDbContext> options) : DbContext(options)
{
    public DbSet<CachedTranslation> CachedTranslations =>
        Set<CachedTranslation>();

    public DbSet<User> Users => Set<User>();

    public DbSet<TranslationUsage> TranslationUsages =>
        Set<TranslationUsage>();

    public DbSet<RefreshToken> RefreshTokens =>
        Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CachedTranslation>(entity =>
        {
            entity.HasIndex(t => new { t.CacheKey, t.Model }).IsUnique();

            entity.Property(t => t.Tone).HasConversion<string>().HasMaxLength(16);

            entity.Property(t => t.CreatedAtUtc).HasColumnType("datetime(6)");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();

            entity.Property(u => u.CreatedAtUtc).HasColumnType("datetime(6)");
        });

        modelBuilder.Entity<TranslationUsage>(entity =>
        {
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(u => u.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(u => u.Source)
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(u => u.Status)
                .HasConversion<string>()
                .HasMaxLength(16);
            entity.Property(u => u.Tone)
                .HasConversion<string>()
                .HasMaxLength(16);

            entity.HasIndex(usage => usage.UserId);
            entity.HasIndex(usage => new { usage.UserId, usage.CreatedAtUtc });
        });
    
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(t => t.TokenHash).IsUnique();
            entity.HasIndex(t => t.FamilyId);

            entity.Property(t => t.RevokedAtUtc).IsConcurrencyToken();

            entity.Property(t => t.CreatedAtUtc).HasColumnType("datetime(6)");
            entity.Property(t => t.ExpiresAtUtc).HasColumnType("datetime(6)");
            entity.Property(t => t.RevokedAtUtc).HasColumnType("datetime(6)");
        });
    
    }
}