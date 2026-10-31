using QikLog.Core;

namespace QikLog.Web.Services;

/// <summary>Dashboard API client. Tail history is the seam used by live-tail tests.</summary>
public interface IQikLogApiClient
{
    /// <summary>Recent stored events for a source.</summary>
    Task<IReadOnlyList<LogEntry>> GetSourceHistoryAsync(
        string source,
        int limit,
        CancellationToken cancellationToken);
}
