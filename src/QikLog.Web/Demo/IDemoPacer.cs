namespace QikLog.Web.Demo;

/// <summary>Pause between simulated lines. Tests record the delay instead of waiting.</summary>
public interface IDemoPacer
{
    /// <summary>Wait for <paramref name="delay"/>, or until <paramref name="cancellationToken"/> fires.</summary>
    Task PauseAsync(TimeSpan delay, CancellationToken cancellationToken);
}

/// <summary>Production pacer. Uses the clock.</summary>
public sealed class DemoPacer : IDemoPacer
{
    /// <inheritdoc />
    public Task PauseAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        Task.Delay(delay, cancellationToken);
}
