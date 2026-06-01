using Microsoft.EntityFrameworkCore;

namespace QikLog.Infrastructure.Data;

public sealed class QikLogDbContext(DbContextOptions<QikLogDbContext> options) : DbContext(options)
{
    public DbSet<LogEntryEntity> LogEntries => Set<LogEntryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
    }
}
