using LogLevel = QikLog.Core.LogLevel;

namespace QikLog.Web.Demo;

/// <summary>Posts one playground line to the ingest API. The credential stays on the server.</summary>
public interface IDemoIngest
{
    /// <summary>Returns true when the API accepts the line with 202.</summary>
    Task<bool> SendAsync(string source, LogLevel level, string message, CancellationToken cancellationToken);
}
