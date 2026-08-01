using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace QikLog.Web.Services;

/// <summary>
/// Prefers the signed-in user's OIDC access token for API calls; falls back to the
/// shared hub API key so anonymous demo tail/history still works in invite beta.
/// </summary>
internal sealed class AccessTokenOrApiKeyHandler(
    IHttpContextAccessor httpContextAccessor,
    IOptions<QikLogOptions> options) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.User.Identity?.IsAuthenticated == true)
        {
            var accessToken = await httpContext.GetTokenAsync("access_token");
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Remove("X-QikLog-API-Key");
                return await base.SendAsync(request, cancellationToken);
            }
        }

        var hubApiKey = options.Value.HubApiKey;
        if (!string.IsNullOrWhiteSpace(hubApiKey)
            && !request.Headers.Contains("X-QikLog-API-Key")
            && request.Headers.Authorization is null)
        {
            request.Headers.Add("X-QikLog-API-Key", hubApiKey.Trim());
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
