using Microsoft.AspNetCore.SignalR;
using QikLog.Api.Observability;

namespace QikLog.Api.Hubs;

/// <summary>
/// SignalR hub for live log streaming. Clients call <see cref="Subscribe"/> with a
/// source name to join a group; the API broadcasts to that group on ingest.
/// </summary>
public sealed class LogHub(ILogger<LogHub> log) : Hub
{
    public override async Task OnConnectedAsync()
    {
        QikLogMetrics.SignalRConnections.Inc();
        log.LogInformation("SignalR client connected {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        QikLogMetrics.SignalRConnections.Dec();
        if (exception is not null)
            log.LogWarning(exception, "SignalR client disconnected with error {ConnectionId}", Context.ConnectionId);
        else
            log.LogInformation("SignalR client disconnected {ConnectionId}", Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }

    public Task Subscribe(string source)
    {
        log.LogInformation("SignalR subscribe {ConnectionId} to source {Source}", Context.ConnectionId, source);
        return Groups.AddToGroupAsync(Context.ConnectionId, $"source:{source}");
    }

    public Task Unsubscribe(string source)
    {
        log.LogInformation("SignalR unsubscribe {ConnectionId} from source {Source}", Context.ConnectionId, source);
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, $"source:{source}");
    }
}
