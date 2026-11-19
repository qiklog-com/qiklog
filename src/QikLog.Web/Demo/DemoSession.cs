using System.Net;
using Microsoft.Extensions.DependencyInjection;

namespace QikLog.Web.Demo;

/// <summary>One visitor's playground: source, budget, and latency. Scoped to the circuit.</summary>
public sealed class DemoSession
{
    /// <summary>Creates a session for the current request's client IP.</summary>
    [ActivatorUtilitiesConstructor]
    public DemoSession(IHttpContextAccessor http)
        : this(DemoSourceNames.Create(), ReadClientIp(http.HttpContext))
    {
    }

    /// <summary>Creates a session with a known source and IP. Tests use this.</summary>
    public DemoSession(string source, string? clientIp)
    {
        Source = source;
        ClientIp = string.IsNullOrWhiteSpace(clientIp) ? null : clientIp;
    }

    /// <summary>Source this circuit subscribes to and sends as.</summary>
    public string Source { get; }

    /// <summary>Client IP after forwarded headers, or null when the request has none.</summary>
    public string? ClientIp { get; }

    /// <summary>Line and cooldown counters.</summary>
    public DemoCircuitBudget Budget { get; } = new();

    /// <summary>Send-to-arrival timings.</summary>
    public DemoLatencyTracker Latency { get; } = new();

    /// <summary>Successful posts this circuit. The nudge appears at 3.</summary>
    public int Accepted { get; set; }

    private static string? ReadClientIp(HttpContext? http)
    {
        var ip = http?.Connection.RemoteIpAddress;
        if (ip is null)
            return null;

        if (ip.IsIPv4MappedToIPv6)
            ip = ip.MapToIPv4();

        return ip.Equals(IPAddress.None) ? null : ip.ToString();
    }
}
