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

        var window = _windows.AddOrUpdate(
            apiKeyId,
            _ => new Window(windowStart, 1),
            (_, existing) =>
            {
                if (existing.Start != windowStart)
                    return new Window(windowStart, 1);
                if (existing.Count >= limitPerMinute)
                    return existing;
                return new Window(windowStart, existing.Count + 1);
            });

        return window.Start == windowStart && window.Count <= limitPerMinute;
    }

    private sealed record Window(DateTimeOffset Start, int Count);
}
