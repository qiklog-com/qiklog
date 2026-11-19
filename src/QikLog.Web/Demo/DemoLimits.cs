namespace QikLog.Web.Demo;

/// <summary>Why a playground send was refused.</summary>
public enum DemoLimitKind
{
    /// <summary>The send is allowed.</summary>
    Ok,

    /// <summary>Manual send sooner than one second after the previous one.</summary>
    SendTooFast,

    /// <summary>Simulate burst sooner than thirty seconds after the previous one.</summary>
    BurstCooling,

    /// <summary>This circuit has used its 60 lines.</summary>
    CircuitCap,

    /// <summary>This client IP has used its 300 lines for the hour.</summary>
    IpCap
}

/// <summary>Result of a limit check. <see cref="Message"/> is empty when allowed.</summary>
public sealed record DemoLimit(bool Allowed, string? Message, DemoLimitKind Kind)
{
    /// <summary>Allowed.</summary>
    public static DemoLimit Ok { get; } = new(true, null, DemoLimitKind.Ok);

    /// <summary>Manual send is inside the one-second gap.</summary>
    public static DemoLimit SendTooFast { get; } = new(false, "Give it a second.", DemoLimitKind.SendTooFast);

    /// <summary>Simulate is inside the thirty-second gap.</summary>
    public static DemoLimit BurstCooling { get; } = new(false, "Simulate is cooling down.", DemoLimitKind.BurstCooling);

    /// <summary>Circuit line budget is exhausted.</summary>
    public static DemoLimit CircuitCap { get; } = new(false, "That's the limit for this visit.", DemoLimitKind.CircuitCap);

    /// <summary>IP line budget is exhausted.</summary>
    public static DemoLimit IpCap { get; } = new(false, "This network is at its hourly limit.", DemoLimitKind.IpCap);
}

/// <summary>Counters for one Blazor circuit.</summary>
public sealed class DemoCircuitBudget
{
    /// <summary>Lines this circuit may send, including simulated ones.</summary>
    public const int MaxLines = 60;

    /// <summary>Lines already reserved.</summary>
    public int Lines { get; set; }

    /// <summary>When the last manual send was reserved.</summary>
    public DateTimeOffset? LastManualSend { get; set; }

    /// <summary>When the last burst was reserved.</summary>
    public DateTimeOffset? LastBurst { get; set; }
}

/// <summary>Rolling hourly line counts keyed by client IP. Shared across circuits.</summary>
public sealed class DemoIpWindow
{
    /// <summary>Lines one IP may send per rolling hour.</summary>
    public const int MaxLinesPerHour = 300;

    private static readonly TimeSpan Window = TimeSpan.FromHours(1);
    private readonly Dictionary<string, Queue<DateTimeOffset>> _hits = new();
    private readonly object _gate = new();

    /// <summary>
    /// Returns false when <paramref name="count"/> does not fit. When
    /// <paramref name="consume"/> is true and it fits, the lines are reserved.
    /// A null IP is not bucketed.
    /// </summary>
    public bool TryReserve(string? ip, DateTimeOffset now, int count, bool consume)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return true;

        lock (_gate)
        {
            if (!_hits.TryGetValue(ip, out var queue))
            {
                queue = new Queue<DateTimeOffset>();
                _hits[ip] = queue;
            }

            var cutoff = now - Window;
            while (queue.Count > 0 && queue.Peek() <= cutoff)
                queue.Dequeue();

            if (queue.Count + count > MaxLinesPerHour)
                return false;

            if (consume)
            {
                for (var i = 0; i < count; i++)
                    queue.Enqueue(now);
            }

            return true;
        }
    }
}

/// <summary>Applies per-circuit and per-IP limits for the playground.</summary>
public sealed class DemoRateGate(DemoSession session, DemoIpWindow window, TimeProvider time)
{
    /// <summary>Lines in one simulate burst.</summary>
    public const int BurstLines = 8;

    private readonly object _circuitLock = new();

    /// <summary>Whether a manual send would be accepted right now.</summary>
    public DemoLimit PeekManual() => Evaluate(1, burst: false, consume: false);

    /// <summary>Reserve one manual line, or explain why not.</summary>
    public DemoLimit TryManual() => Evaluate(1, burst: false, consume: true);

    /// <summary>Whether a full burst would be accepted right now.</summary>
    public DemoLimit PeekBurst() => Evaluate(BurstLines, burst: true, consume: false);

    /// <summary>Reserve a full burst, or explain why not. A short budget refuses the whole burst.</summary>
    public DemoLimit TryBurst() => Evaluate(BurstLines, burst: true, consume: true);

    private DemoLimit Evaluate(int count, bool burst, bool consume)
    {
        var now = time.GetUtcNow();
        var budget = session.Budget;
        lock (_circuitLock)
        {
            if (budget.Lines + count > DemoCircuitBudget.MaxLines)
                return DemoLimit.CircuitCap;

            if (!window.TryReserve(session.ClientIp, now, count, consume: false))
                return DemoLimit.IpCap;

            if (burst)
            {
                if (budget.LastBurst is { } last && now - last < TimeSpan.FromSeconds(30))
                    return DemoLimit.BurstCooling;
            }
            else if (budget.LastManualSend is { } last && now - last < TimeSpan.FromSeconds(1))
            {
                return DemoLimit.SendTooFast;
            }

            if (!consume)
                return DemoLimit.Ok;

            if (!window.TryReserve(session.ClientIp, now, count, consume: true))
                return DemoLimit.IpCap;

            budget.Lines += count;
            if (burst)
                budget.LastBurst = now;
            else
                budget.LastManualSend = now;

            return DemoLimit.Ok;
        }
    }
}
