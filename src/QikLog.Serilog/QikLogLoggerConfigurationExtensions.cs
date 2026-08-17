using System.Net.Http.Headers;
using Serilog.Configuration;
using Serilog.Sinks.PeriodicBatching;
using QikLog.Serilog;

namespace Serilog;

/// <summary>Extensions that wire QikLog ingest into <c>WriteTo</c>.</summary>
public static class QikLogLoggerConfigurationExtensions
{
    /// <summary>
    /// Send log events to QikLog via <c>POST /v1/logs</c> (same JSON and Bearer
    /// key as <c>qiklog send</c>). Failures are written to Serilog SelfLog only.
    /// </summary>
    /// <param name="loggerSinkConfiguration">The Serilog <c>WriteTo</c> configuration.</param>
    /// <param name="apiUrl">API origin, e.g. <c>https://api.qiklog.com</c>.</param>
    /// <param name="apiKey">Ingest key. Sent as <c>Authorization: Bearer</c>.</param>
    /// <param name="source">QikLog source name (e.g. <c>demo</c>).</param>
    /// <param name="batchSizeLimit">Max events per flush. Default 50.</param>
    /// <param name="flushInterval">Max wait before a partial batch flushes. Default 2 seconds.</param>
    /// <returns>The logger configuration, for chaining.</returns>
    public static LoggerConfiguration QikLog(
        this LoggerSinkConfiguration loggerSinkConfiguration,
        string apiUrl,
        string apiKey,
        string source,
        int batchSizeLimit = 50,
        TimeSpan? flushInterval = null)
    {
        ArgumentNullException.ThrowIfNull(loggerSinkConfiguration);
        var http = CreateClient(apiUrl, apiKey, handler: null);
        return Attach(loggerSinkConfiguration, http, source, ownsHttp: true, batchSizeLimit, flushInterval);
    }

    /// <summary>Test hook: inject an <see cref="HttpMessageHandler"/>.</summary>
    internal static LoggerConfiguration QikLog(
        this LoggerSinkConfiguration loggerSinkConfiguration,
        string apiUrl,
        string apiKey,
        string source,
        HttpMessageHandler handler,
        int batchSizeLimit,
        TimeSpan flushInterval)
    {
        ArgumentNullException.ThrowIfNull(loggerSinkConfiguration);
        ArgumentNullException.ThrowIfNull(handler);
        var http = CreateClient(apiUrl, apiKey, handler);
        return Attach(loggerSinkConfiguration, http, source, ownsHttp: true, batchSizeLimit, flushInterval);
    }

    private static HttpClient CreateClient(string apiUrl, string apiKey, HttpMessageHandler? handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiUrl);

        var http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
        http.BaseAddress = new Uri(apiUrl.TrimEnd('/') + "/", UriKind.Absolute);
        http.Timeout = TimeSpan.FromSeconds(10);
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var key = apiKey?.Trim();
        if (!string.IsNullOrWhiteSpace(key))
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);

        return http;
    }

    private static LoggerConfiguration Attach(
        LoggerSinkConfiguration loggerSinkConfiguration,
        HttpClient http,
        string source,
        bool ownsHttp,
        int batchSizeLimit,
        TimeSpan? flushInterval)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        var batched = new QikLogBatchedSink(http, source.Trim(), ownsHttp);
        var options = new PeriodicBatchingSinkOptions
        {
            BatchSizeLimit = batchSizeLimit < 1 ? 1 : batchSizeLimit,
            Period = flushInterval ?? TimeSpan.FromSeconds(2),
            QueueLimit = 10_000,
            EagerlyEmitFirstEvent = true
        };

        return loggerSinkConfiguration.Sink(new PeriodicBatchingSink(batched, options));
    }
}
