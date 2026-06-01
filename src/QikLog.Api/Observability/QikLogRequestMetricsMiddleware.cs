using System.Diagnostics;

namespace QikLog.Api.Observability;

/// <summary>Records per-endpoint HTTP request counts and duration histograms.</summary>
internal sealed class QikLogRequestMetricsMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> ExcludedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/metrics",
        "/health",
        "/healthz"
    };

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        if (ExcludedPaths.Contains(path))
        {
            await next(context);
            return;
        }

        var endpoint = ResolveEndpointLabel(context);
        var method = context.Request.Method;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            QikLogMetrics.HttpRequests.WithLabels(method, endpoint).Inc();
            QikLogMetrics.HttpRequestDuration
                .WithLabels(method, endpoint)
                .Observe(stopwatch.Elapsed.TotalSeconds);
        }
    }

    private static string ResolveEndpointLabel(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint?.DisplayName is { Length: > 0 } displayName)
            return displayName;

        return context.Request.Path.Value ?? "unknown";
    }
}
