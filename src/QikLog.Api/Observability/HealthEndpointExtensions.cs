using System.Net.Sockets;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using QikLog.Infrastructure;
using QikLog.Infrastructure.Data;

namespace QikLog.Api.Observability;

internal static class HealthEndpointExtensions
{
    public static void MapQikLogHealth(this WebApplication app)
    {
        app.MapGet("/health", async (IServiceProvider sp, IConfiguration config, CancellationToken ct) =>
        {
            var store = sp.GetRequiredService<ILogEntryStore>();
            var postgres = await CheckPostgresAsync(store, sp, ct);
            var redis = await CheckRedisAsync(config.GetConnectionString("Redis"), ct);
            var version = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? "unknown";

            var healthy = postgres is "ok" or "skipped" && redis is "ok" or "skipped";
            var body = new HealthResponse(
                healthy ? "ok" : "degraded",
                version,
                postgres,
                redis);

            return healthy
                ? Results.Ok(body)
                : Results.Json(body, statusCode: StatusCodes.Status503ServiceUnavailable);
        })
        .WithName("DetailedHealth")
        .WithSummary("Detailed health check")
        .WithDescription(
            "Returns service version and dependency status for Postgres and Redis. " +
            "Use /healthz for simple orchestrator probes.");
    }

    private static async Task<string> CheckPostgresAsync(
        ILogEntryStore store,
        IServiceProvider sp,
        CancellationToken cancellationToken)
    {
        if (!store.IsEnabled)
            return "skipped";

        try
        {
            await using var scope = sp.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<QikLogDbContext>();
            return await db.Database.CanConnectAsync(cancellationToken) ? "ok" : "unreachable";
        }
        catch (Exception ex)
        {
            var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("QikLog.Health");
            log.LogError(ex, "Postgres health check failed");
            return "error";
        }
    }

    private static async Task<string> CheckRedisAsync(string? connectionString, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return "skipped";

        if (!TryParseRedisHostPort(connectionString, out var host, out var port))
            return "skipped";

        try
        {
            using var tcp = new TcpClient();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(2));
            await tcp.ConnectAsync(host, port, timeout.Token);
            return tcp.Connected ? "ok" : "unreachable";
        }
        catch (Exception)
        {
            return "unreachable";
        }
    }

    private static bool TryParseRedisHostPort(string connectionString, out string host, out int port)
    {
        host = "";
        port = 6379;

        var value = connectionString.Trim();
        if (value.Contains("://", StringComparison.Ordinal))
        {
            var uri = new Uri(value);
            host = uri.Host;
            port = uri.Port > 0 ? uri.Port : 6379;
            return host.Length > 0;
        }

        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var endpoint = parts[0];
        var hostPort = endpoint.Split(':', StringSplitOptions.TrimEntries);
        host = hostPort[0];
        if (hostPort.Length > 1 && int.TryParse(hostPort[1], out var parsedPort))
            port = parsedPort;

        return host.Length > 0;
    }
}

/// <summary>Response body for GET /health.</summary>
public sealed record HealthResponse(string Status, string Version, string Postgres, string Redis);
