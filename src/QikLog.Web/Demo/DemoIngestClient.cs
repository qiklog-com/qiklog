using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using LogLevel = QikLog.Core.LogLevel;

namespace QikLog.Web.Demo;

/// <summary>Server-side ingest for the try-it-now page. Does not use the visitor's OIDC token.</summary>
public sealed class DemoIngestClient(
    HttpClient http,
    IOptions<QikLogOptions> options,
    ILogger<DemoIngestClient> log) : IDemoIngest
{
    /// <inheritdoc />
    public async Task<bool> SendAsync(
        string source,
        LogLevel level,
        string message,
        CancellationToken cancellationToken)
    {
        var key = DemoCredentials.Resolve(options.Value);
        if (string.IsNullOrWhiteSpace(key))
        {
            log.LogWarning("Demo ingest skipped for {Source}: demo credential is not configured", source);
            return false;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/logs");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        request.Content = JsonContent.Create(new
        {
            source,
            level = DemoLevels.Wire(level),
            message
        });

        var response = await http.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Accepted)
        {
            log.LogWarning(
                "Demo ingest rejected for {Source}: {Status}",
                source,
                (int)response.StatusCode);
            return false;
        }

        return true;
    }
}
