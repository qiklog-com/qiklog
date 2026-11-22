using System.Collections.Concurrent;

namespace QikLog.Api.Links;

/// <summary>
/// Per-address fixed-window limit on new log links (in-process, like
/// <see cref="QikLog.Infrastructure.Auth.ApiKeyRateLimiter"/>). Creating a link needs no
/// credential, so this is what keeps one address from minting them without end.
/// </summary>
public sealed class LinkCreateLimiter(TimeProvider time)
{
    private const int PruneThreshold = 5000;

    private readonly ConcurrentDictionary<string, Window> _windows = new(StringComparer.Ordinal);

    public bool TryAcquire(string clientKey, int limitPerHour)
    {
        if (limitPerHour <= 0)
            return true;

        var now = time.GetUtcNow();
        var windowStart = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero);

        if (_windows.Count > PruneThreshold)
            Prune(windowStart);

        while (true)
        {
            var current = _windows.GetOrAdd(clientKey, _ => new Window(windowStart, 0));

            Window next;
            if (current.Start != windowStart)
                next = new Window(windowStart, 1);
            else if (current.Count >= limitPerHour)
                return false;
            else
                next = new Window(windowStart, current.Count + 1);

            if (_windows.TryUpdate(clientKey, next, current))
                return true;
        }
    }

    private void Prune(DateTimeOffset windowStart)
    {
        foreach (var pair in _windows)
        {
            if (pair.Value.Start != windowStart)
                _windows.TryRemove(pair);
        }
    }

    private sealed record Window(DateTimeOffset Start, int Count);
}
