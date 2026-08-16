using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using QikLog.Core;

namespace QikLog.Cli;

/// <summary>
/// SignalR live-tail session: connect to /hubs/logs, Subscribe(source), print LogReceived.
/// Hub Subscribe does not replay history; only lines ingested after subscribe arrive.
/// </summary>
internal sealed class WatchSession
{
    public static async Task<int> RunAsync(
        string apiBaseUrl,
        string source,
        string? apiKey,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        var key = ResolveApiKey(apiKey);
        if (string.IsNullOrWhiteSpace(key))
        {
            error.WriteLine("missing API key: pass --key or set QIKLOG_API_KEY");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            error.WriteLine("source is required");
            return 1;
        }

        var hubUrl = $"{apiBaseUrl.TrimEnd('/')}/hubs/logs";
        await using var connection = BuildConnection(hubUrl, key);

        connection.On<LogEntry>("LogReceived", entry =>
        {
            output.WriteLine(LogLineFormatter.Format(entry));
        });

        try
        {
            await connection.StartAsync(cancellationToken);
            await connection.InvokeAsync("Subscribe", source, cancellationToken);

            output.WriteLine($"watching source={source} hub={hubUrl}");
            output.WriteLine("press Ctrl+C to stop (no history replay; live lines only)");

            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Ctrl+C / shutdown — exit cleanly
        }
        catch (Exception ex)
        {
            error.WriteLine($"watch failed: {DescribeConnectFailure(ex)}");
            await StopQuietlyAsync(connection);
            return 1;
        }

        await StopQuietlyAsync(connection);
        return 0;
    }

    /// <summary>
    /// Builds a hub connection authenticated with the API key (Bearer + header),
    /// matching ingest credentials and the browser Tail fallback key header.
    /// </summary>
    internal static HubConnection BuildConnection(string hubUrl, string apiKey) =>
        new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.Headers["Authorization"] = $"Bearer {apiKey}";
                options.Headers["X-QikLog-API-Key"] = apiKey;
                options.Transports = HttpTransportType.WebSockets | HttpTransportType.LongPolling;
            })
            .WithAutomaticReconnect()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(new LogLevelJsonConverter());
                options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
            })
            .Build();

    internal static string? ResolveApiKey(string? keyOption) =>
        !string.IsNullOrWhiteSpace(keyOption)
            ? keyOption.Trim()
            : Environment.GetEnvironmentVariable("QIKLOG_API_KEY")?.Trim();

    private static string DescribeConnectFailure(Exception ex)
    {
        var message = ex.Message;
        if (ex.InnerException is not null)
            message = $"{message} ({ex.InnerException.Message})";

        if (message.Contains("401", StringComparison.Ordinal)
            || message.Contains("403", StringComparison.Ordinal)
            || message.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Forbidden", StringComparison.OrdinalIgnoreCase))
            return "authentication rejected (check --key / QIKLOG_API_KEY)";

        return message;
    }

    private static async Task StopQuietlyAsync(HubConnection connection)
    {
        try
        {
            await connection.StopAsync();
        }
        catch
        {
            // already closed
        }
    }
}
