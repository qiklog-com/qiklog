namespace QikLog.Infrastructure.Tests;

/// <summary>A clock that only moves when a test moves it.</summary>
internal sealed class ManualTimeProvider(DateTimeOffset start) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = start;

    public override DateTimeOffset GetUtcNow() => Now;
}
