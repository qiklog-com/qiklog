using Microsoft.EntityFrameworkCore;

namespace QikLog.Infrastructure.Data;

public sealed class QikLogDbContext(DbContextOptions<QikLogDbContext> options) : DbContext(options)
{
    public DbSet<LogEntryEntity> LogEntries => Set<LogEntryEntity>();

    public DbSet<ApiKeyEntity> ApiKeys => Set<ApiKeyEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var apiKey = modelBuilder.Entity<ApiKeyEntity>();
        apiKey.ToTable("api_keys");
        apiKey.HasKey(k => k.Id);
        apiKey.Property(k => k.Name).HasMaxLength(128).IsRequired();
        apiKey.Property(k => k.LookupPrefix).HasMaxLength(8).IsRequired();
        apiKey.Property(k => k.SecretHash).HasMaxLength(256).IsRequired();
        apiKey.HasIndex(k => k.LookupPrefix);
        apiKey.HasIndex(k => new { k.IsActive, k.RevokedAt });

        var entity = modelBuilder.Entity<LogEntryEntity>();

        entity.ToTable("log_entries");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id)
            .UseIdentityAlwaysColumn();

        entity.Property(e => e.Source)
            .HasMaxLength(128)
            .IsRequired();

        entity.Property(e => e.Level)
            .HasConversion<short>();

        entity.Property(e => e.Message)
            .IsRequired();

        entity.Property(e => e.Timestamp)
            .HasColumnType("timestamptz");

        entity.Property(e => e.ReceivedAt)
            .HasColumnType("timestamptz");

        entity.Property(e => e.PropertiesJson)
            .HasColumnType("jsonb");

        entity.HasIndex(e => new { e.Source, e.ReceivedAt });

        entity.Property(e => e.ApiKeyId);
        entity.HasOne(e => e.ApiKey)
            .WithMany()
            .HasForeignKey(e => e.ApiKeyId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
