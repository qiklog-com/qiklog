using QikLog.Core;

namespace QikLog.Web.Stage;

/// <summary>In-memory Stage rows: history load, live arrival flags, and the cap.</summary>
internal sealed class StageBuffer
{
    public const int Cap = 500;

    private readonly List<StageLogRow> _rows = [];
    private long _sequence;

    public IReadOnlyList<StageLogRow> Rows => _rows;

    public int Count => _rows.Count;

    public void LoadHistory(IEnumerable<LogEntry> history)
    {
        foreach (var entry in history)
            _rows.Add(new StageLogRow(++_sequence, entry));

        TrimToCap();
    }

    public void AppendLive(IEnumerable<LogEntry> batch)
    {
        var appended = 0;
        var wasEmpty = _rows.Count == 0;
        foreach (var entry in batch)
        {
            var opening = wasEmpty && appended == 0;
            _rows.Add(new StageLogRow(++_sequence, entry, Arrive: !opening, Opening: opening));
            appended++;
        }

        TrimToCap();
    }

    public void Clear()
    {
        _rows.Clear();
        _sequence = 0;
    }

    private void TrimToCap()
    {
        if (_rows.Count > Cap)
            _rows.RemoveRange(0, _rows.Count - Cap);
    }
}
