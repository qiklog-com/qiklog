using QikLog.Core;

namespace QikLog.Web.Stage;

/// <summary>One buffered row on the Stage, with live-arrival flags.</summary>
internal sealed record StageLogRow(
    long Sequence,
    LogEntry Entry,
    bool Arrive = false,
    bool Opening = false);
