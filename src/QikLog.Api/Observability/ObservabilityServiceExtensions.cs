using Prometheus;

namespace QikLog.Api.Observability;

internal static class ObservabilityServiceExtensions
{
    public static WebApplication UseQikLogObservability(this WebApplication app)
    {
        app.UseMiddleware<QikLogRequestMetricsMiddleware>();
        app.UseHttpMetrics();
        return app;
    }

    public static WebApplication MapQikLogObservability(this WebApplication app)
    {
        app.MapQikLogHealth();
        app.MapMetrics("/metrics");
        return app;
    }
}
