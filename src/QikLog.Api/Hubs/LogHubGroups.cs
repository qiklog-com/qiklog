namespace QikLog.Api.Hubs;

/// <summary>SignalR group naming for tenant-isolated live tail.</summary>
internal static class LogHubGroups
{
    public static string ForSource(string source) => $"source:{source}";

    public static string ForTenantSource(Guid tenantId, string source) => $"tenant:{tenantId}:source:{source}";

    /// <summary>Viewers of one anonymous log link. Not tenant-scoped: the link id is the secret.</summary>
    public static string ForLink(string linkId) => $"link:{linkId}";
}
