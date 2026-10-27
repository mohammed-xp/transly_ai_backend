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

    public DbSet<Tone> Tones => Set<Tone>();

    public DbSet<ConversationUsage> ConversationUsages => Set<ConversationUsage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tone>(entity =>
        {
            entity.Property(t => t.Code).HasMaxLength(32);
            entity.Property(t => t.PromptInstruction).HasMaxLength(1000);

            entity.HasIndex(t => t.Code).IsUnique();
            entity.HasIndex(t => t.IsDefault).IsUnique();

            entity.HasMany(t => t.Translations)
                .WithOne()
                .HasForeignKey(x => x.ToneId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ToneTranslation>(entity =>
        {
            entity.ToTable("ToneTranslations");
            entity.HasKey(x => new { x.ToneId, x.Culture });

            entity.Property(x => x.Culture).HasMaxLength(16);
            entity.Property(x => x.Name).HasMaxLength(64);
            entity.Property(x => x.Description).HasMaxLength(256);
        });

        modelBuilder.Entity<CachedTranslation>(entity =>
        {
            entity.HasIndex(t => new { t.CacheKey, t.Model }).IsUnique();

            entity.HasOne<Tone>()
                .WithMany()
                .HasForeignKey(t => t.ToneId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(t => t.CreatedAtUtc).HasColumnType("datetime(6)");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();

            entity.HasIndex(u => u.GoogleSubject).IsUnique();

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
            entity.HasOne<Tone>()
                .WithMany()
                .HasForeignKey(u => u.ToneId)
                .OnDelete(DeleteBehavior.Restrict);

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

        modelBuilder.Entity<ConversationUsage>(entity =>
            {
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(u => u.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(u => u.Status)
                    .HasConversion<string>()
                    .HasMaxLength(16);
                
                entity.HasIndex(u => new {u.UserId, u.CreatedAtUtc});
            });
    }
}