using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using QikLog.Api.Auth;
using QikLog.Api.Observability;
using QikLog.Infrastructure;
using QikLog.Infrastructure.Auth;

namespace QikLog.Api.Hubs;

/// <summary>
/// SignalR hub for live log streaming. Clients call <see cref="Subscribe"/> with a
/// source name to join a tenant-scoped group; the API broadcasts on ingest.
/// </summary>
public sealed class LogHub(
    TenantAuthenticationService authentication,
    IOptions<AuthEnforcementOptions> enforcementOptions,
    ILogger<LogHub> log) : Hub
{
    internal const string TenantIdItemKey = "TenantId";

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var store = httpContext?.RequestServices.GetService<ILogEntryStore>();

        if (httpContext?.Items.TryGetValue(TenantIdItemKey, out var existingTenant) == true
            && existingTenant is Guid tenantFromMiddleware)
        {
            Context.Items[TenantIdItemKey] = tenantFromMiddleware;
        }
        else if (httpContext is not null
            && store is { IsEnabled: true }
            && enforcementOptions.Value.Enabled)
        {
            var (success, failure) = await authentication.AuthenticateAsync(
                httpContext,
                AuthMode.JwtOrApiKey,
                applyIngestRateLimit: false,
                Context.ConnectionAborted);

            if (success is null)
            {
                log.LogWarning(
                    "SignalR connection refused for {ConnectionId}: {Failure}",
                    Context.ConnectionId,
                    failure);
                Context.Abort();
                return;
            }

            Context.Items[TenantIdItemKey] = success.TenantId;
        }

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
        var group = ResolveGroupName(source);
        log.LogInformation(
            "SignalR subscribe {ConnectionId} to {Group}",
            Context.ConnectionId,
            group);
        return Groups.AddToGroupAsync(Context.ConnectionId, group);
    }

    public Task Unsubscribe(string source)
    {
        var group = ResolveGroupName(source);
        log.LogInformation(
            "SignalR unsubscribe {ConnectionId} from {Group}",
            Context.ConnectionId,
            group);
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
    }

    private string ResolveGroupName(string source)
    {
        if (Context.Items.TryGetValue(TenantIdItemKey, out var value) && value is Guid tenantId)
            return LogHubGroups.ForTenantSource(tenantId, source);

        return LogHubGroups.ForSource(source);
    }
}
