using LogLevel = QikLog.Core.LogLevel;

namespace QikLog.Web.Demo;

/// <summary>Matches sends to SignalR arrivals and keeps the session's fastest time.</summary>
public sealed class DemoLatencyTracker
{
    private readonly object _gate = new();
    private readonly List<Pending> _waiting = [];
    private readonly List<Noted> _notes = [];

    /// <summary>Fastest arrival this session, in whole milliseconds.</summary>
    public int? BestMs { get; private set; }

    /// <summary>Remember a send so the matching arrival can be timed.</summary>
    public void MarkSent(LogLevel level, string message, long timestamp)
    {
        lock (_gate)
            _waiting.Add(new Pending(level, message, timestamp));
    }

    /// <summary>Record latency when a matching line arrives. Unmatched lines are ignored.</summary>
    public void Observe(LogLevel level, string message, long timestamp, TimeProvider time)
    {
        lock (_gate)
        {
            var index = _waiting.FindIndex(item => item.Level == level && item.Message == message);
            if (index < 0)
                return;

            var pending = _waiting[index];
            _waiting.RemoveAt(index);
            var elapsed = time.GetElapsedTime(pending.Timestamp, timestamp);
            var ms = (int)Math.Max(0, Math.Round(elapsed.TotalMilliseconds));
            _notes.Add(new Noted(level, message, ms));
            if (BestMs is null || ms < BestMs)
                BestMs = ms;
        }
    }

    /// <summary>Take the next unused note for this line, or null when it was not one we sent.</summary>
    public string? Take(LogLevel level, string message)
    {
        lock (_gate)
        {
            var index = _notes.FindIndex(item => !item.Taken && item.Level == level && item.Message == message);
            if (index < 0)
                return null;

            var note = _notes[index];
            _notes[index] = note with { Taken = true };
            return $"arrived in {note.Ms} ms";
        }
    }

    private readonly record struct Pending(LogLevel Level, string Message, long Timestamp);

    private readonly record struct Noted(LogLevel Level, string Message, int Ms, bool Taken = false);
}
