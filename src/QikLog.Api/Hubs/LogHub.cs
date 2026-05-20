using Microsoft.AspNetCore.SignalR;

namespace QikLog.Api.Hubs;

/// <summary>
/// SignalR hub for live log streaming. Clients call <see cref="Subscribe"/> with a
/// source name to join a group; the API broadcasts to that group on ingest.
/// </summary>
public sealed class LogHub : Hub
{
    public Task Subscribe(string source) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"source:{source}");

    public Task Unsubscribe(string source) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"source:{source}");
}
