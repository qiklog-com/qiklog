using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QikLog.Core;
using QikLog.Infrastructure.Data;

namespace QikLog.Infrastructure;

/// <summary>Persists ingested <see cref="LogEntry"/> rows when a database is configured.</summary>
public interface ILogEntryStore
{
    /// <summary>True when ingest should write to storage (Postgres or test in-memory DB).</summary>
    bool IsEnabled { get; }

    Task SaveAsync(LogEntry entry, CancellationToken cancellationToken);
}

/// <summary>No-op store when no connection string is configured.</summary>
public sealed class NullLogEntryStore : ILogEntryStore
{
    public bool IsEnabled => false;

    public Task SaveAsync(LogEntry entry, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class EfLogEntryStore(
    QikLogDbContext db,
    ILogger<EfLogEntryStore> log) : ILogEntryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public bool IsEnabled => true;

    public async Task SaveAsync(LogEntry entry, CancellationToken cancellationToken)
    {
        var receivedAt = DateTimeOffset.UtcNow;
        var entity = new LogEntryEntity
        {
            Source = entry.Source,
            Level = entry.Level,
            Message = entry.Message,
            Timestamp = entry.Timestamp,
            ReceivedAt = receivedAt,
            PropertiesJson = entry.Properties is null or { Count: 0 }
                ? null
                : JsonSerializer.Serialize(entry.Properties, JsonOptions)
        };

        db.LogEntries.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        log.LogDebug(
            "Persisted log entry {LogEntryId} for source {Source}",
            entity.Id,
            entry.Source);
    }
}
