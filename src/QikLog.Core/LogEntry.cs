namespace QikLog.Core;

/// <summary>
/// A single log event ingested by QikLog. This is the canonical shape on the wire,
/// in storage, and on the dashboard. Keep it small and stable.
/// </summary>
/// <param name="Source">Logical source name (e.g. "api-prod", "worker-01"). Tenant-scoped.</param>
/// <param name="Level">Severity level. See <see cref="LogLevel"/>.</param>
/// <param name="Message">Human-readable message body.</param>
/// <param name="Timestamp">When the event occurred. UTC. Defaults to server-receipt time if omitted by client.</param>
/// <param name="Properties">Optional structured properties. Kept small for v1 (no nested objects).</param>
public sealed record LogEntry(
    string Source,
    LogLevel Level,
    string Message,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, string>? Properties = null
);

public enum LogLevel
{
    Trace = 0,
    Debug = 1,
    Info = 2,
    Warning = 3,
    Error = 4,
    Critical = 5
}
