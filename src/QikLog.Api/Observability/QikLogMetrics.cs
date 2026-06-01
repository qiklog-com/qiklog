using Prometheus;

namespace QikLog.Api.Observability;

/// <summary>Prometheus metrics for QikLog API self-monitoring.</summary>
internal static class QikLogMetrics
{
    public static readonly Counter HttpRequests = Metrics.CreateCounter(
        "qiklog_http_requests_total",
        "HTTP requests received by the API.",
        new CounterConfiguration { LabelNames = ["method", "endpoint"] });

    public static readonly Histogram HttpRequestDuration = Metrics.CreateHistogram(
        "qiklog_http_request_duration_seconds",
        "HTTP request duration in seconds.",
        new HistogramConfiguration
        {
            LabelNames = ["method", "endpoint"],
            Buckets = Histogram.ExponentialBuckets(0.001, 2, 16)
        });

    public static readonly Gauge SignalRConnections = Metrics.CreateGauge(
        "qiklog_signalr_connections_active",
        "Active SignalR connections on the log streaming hub.");

    public static readonly Counter LogsIngested = Metrics.CreateCounter(
        "qiklog_logs_ingested_total",
        "Log entries accepted via POST /v1/logs.");

    public static readonly Counter UsageLimitChecks = Metrics.CreateCounter(
        "qiklog_usage_limit_checks_total",
        "Billing tier usage limit checks before ingest.",
        new CounterConfiguration { LabelNames = ["result"] });
}
