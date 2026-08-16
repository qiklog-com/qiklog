using System.Net.Http.Headers;
using System.Net.Http.Json;
using QikLog.Core;

namespace QikLog.Cli;

/// <summary>HTTP ingest used by <c>qiklog send</c> (and timing smoke).</summary>
internal static class LogSender
{
    public static async Task<SendResult> SendAsync(
        string apiBaseUrl,
        string? apiKey,
        string source,
        string message,
        LogLevel level,
        CancellationToken cancellationToken)
    {
        using var http = new HttpClient();
        ApplyApiKey(http, apiKey);

        var api = apiBaseUrl.TrimEnd('/');
        var payload = new
        {
            source,
            message,
            level = (int)level,
            timestamp = DateTimeOffset.UtcNow
        };

        var response = await http.PostAsJsonAsync($"{api}/v1/logs", payload, cancellationToken);
        if (response.IsSuccessStatusCode)
            return new SendResult(0, null);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return new SendResult(1, $"failed: {(int)response.StatusCode} {response.ReasonPhrase} {body}");
    }

    internal static void ApplyApiKey(HttpClient http, string? key)
    {
        var value = key ?? Environment.GetEnvironmentVariable("QIKLOG_API_KEY");
        if (!string.IsNullOrWhiteSpace(value))
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", value.Trim());
    }
}

internal sealed record SendResult(int ExitCode, string? Error);
