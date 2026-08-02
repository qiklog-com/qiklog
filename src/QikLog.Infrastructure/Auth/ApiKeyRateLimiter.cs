using System.Collections.Concurrent;

namespace QikLog.Infrastructure.Auth;

/// <summary>Per-API-key fixed-window rate limiter (in-process; good enough for MVP).</summary>
public sealed class ApiKeyRateLimiter
{
    private readonly ConcurrentDictionary<Guid, Window> _windows = new();

    public bool TryAcquire(Guid apiKeyId, int limitPerMinute)
    {
        if (limitPerMinute <= 0)
            return true;

        var now = DateTimeOffset.UtcNow;
        var windowStart = new DateTimeOffset(
            now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, TimeSpan.Zero);

        while (true)
        {
            var current = _windows.GetOrAdd(apiKeyId, _ => new Window(windowStart, 0));

            Window next;
            if (current.Start != windowStart)
            {
                next = new Window(windowStart, 1);
            }
            else if (current.Count >= limitPerMinute)
            {
                return false;
            }
            else
            {
                next = new Window(windowStart, current.Count + 1);
            }

            if (_windows.TryUpdate(apiKeyId, next, current))
                return true;
        }
    }

    private sealed record Window(DateTimeOffset Start, int Count);
}
