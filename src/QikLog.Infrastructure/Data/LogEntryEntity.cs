namespace QikLog.Infrastructure.Data;

/// <summary>
/// Persisted log row. Maps to <see cref="Core.LogEntry"/> on the wire.
/// Tenant/source FKs come in a later migration (#10 full schema).
/// </summary>
public sealed class LogEntryEntity
{
    public long Id { get; set; }

    public required string Source { get; set; }

    public Core.LogLevel Level { get; set; }

    public required string Message { get; set; }

    /// <summary>Client or server event time (UTC).</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>When the API accepted the entry (UTC). Used for ordering and future partitioning.</summary>
    public DateTimeOffset ReceivedAt { get; set; }

    /// <summary>Flat string properties as JSON object, or null.</summary>
    public string? PropertiesJson { get; set; }
}
