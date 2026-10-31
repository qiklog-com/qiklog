using Microsoft.AspNetCore.SignalR.Client;
using LogLevel = QikLog.Core.LogLevel;

namespace QikLog.Web.Stage;

/// <summary>Level CSS classes, row classes, and hub status labels for the Stage.</summary>
internal static class StagePresentation
{
    public static string LevelLabel(LogLevel level) => level switch
    {
        LogLevel.Trace => "TRACE",
        LogLevel.Debug => "DEBUG",
        LogLevel.Info => "INFO",
        LogLevel.Warning => "WARN",
        LogLevel.Error => "ERROR",
        LogLevel.Critical => "CRIT",
        _ => level.ToString().ToUpperInvariant()
    };

    public static string LevelClass(LogLevel level) => level switch
    {
        LogLevel.Warning => "ql-stage-lvl ql-stage-lvl-wrn",
        LogLevel.Error or LogLevel.Critical => "ql-stage-lvl ql-stage-lvl-err",
        LogLevel.Info => "ql-stage-lvl ql-stage-lvl-inf",
        _ => "ql-stage-lvl ql-stage-lvl-dbg"
    };

    public static string LevelTone(LogLevel level) => level switch
    {
        LogLevel.Warning => "wrn",
        LogLevel.Error or LogLevel.Critical => "err",
        LogLevel.Info => "inf",
        _ => "dbg"
    };

    public static string RowClass(StageLogRow row)
    {
        var tone = row.Entry.Level switch
        {
            LogLevel.Warning => "ql-stage-row-wrn",
            LogLevel.Error or LogLevel.Critical => "ql-stage-row-err",
            _ => ""
        };

        var extras = row.Opening
            ? "ql-stage-first"
            : row.Arrive ? "ql-stage-row-arrive" : "";

        return $"ql-stage-row {tone} {extras}".Trim();
    }

    public static StageLiveStatus MapHubState(HubConnectionState? state) => state switch
    {
        HubConnectionState.Connected => new StageLiveStatus("LIVE", "connected"),
        HubConnectionState.Connecting or HubConnectionState.Reconnecting =>
            new StageLiveStatus("RECONNECTING", "reconnecting"),
        _ => new StageLiveStatus("PAUSED", "disconnected")
    };
}

/// <summary>LIVE pill copy and <c>data-state</c> for a hub connection.</summary>
internal sealed record StageLiveStatus(string Label, string State);
