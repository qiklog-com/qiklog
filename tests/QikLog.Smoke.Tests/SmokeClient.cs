using System.Net;

namespace QikLog.Smoke.Tests;

/// <summary>
/// Thin HTTP helper for smoke tests. Redirects are never followed so tests can
/// assert on <c>Location</c>, and cold-start timeouts are retried a few times
/// because container platforms may need to wake an idle instance.
/// </summary>
public static class SmokeClient
{
    private static readonly HttpClient Http = CreateClient();

    private const int MaxAttempts = 3;

    private static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler { AllowAutoRedirect = false };
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
    }

    /// <summary>Issues a GET and returns the response without following redirects.</summary>
    public static Task<HttpResponseMessage> GetAsync(string url, CancellationToken cancellationToken = default) =>
        SendAsync(() => new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);

    /// <summary>Issues a POST with an optional JSON body and returns the response.</summary>
    public static Task<HttpResponseMessage> PostJsonAsync(
        string url,
        string? json = null,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            () => new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = json is null
                    ? null
                    : new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            },
            cancellationToken);

    private static async Task<HttpResponseMessage> SendAsync(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var response = await Http.SendAsync(requestFactory(), cancellationToken);

                // 502/503/504 from the platform edge usually means the instance is
                // still waking; a real application fault surfaces as 500.
                if (attempt < MaxAttempts && IsColdStart(response.StatusCode))
                {
                    response.Dispose();
                    await Task.Delay(TimeSpan.FromSeconds(3 * attempt), cancellationToken);
                    continue;
                }

                return response;
            }
            catch (Exception ex) when (attempt < MaxAttempts && ex is HttpRequestException or TaskCanceledException)
            {
                await Task.Delay(TimeSpan.FromSeconds(3 * attempt), cancellationToken);
            }
        }
    }

    private static bool IsColdStart(HttpStatusCode status) =>
        status is HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
}
