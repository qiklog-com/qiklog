using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace QikLog.Web.Services;

/// <summary>
/// Attaches the credential chosen by <see cref="ApiCredentialPolicy"/>: the signed-in
/// user's OIDC access token, or the shared hub API key for anonymous demo viewing only.
/// </summary>
internal sealed class AccessTokenOrApiKeyHandler(
    IHttpContextAccessor httpContextAccessor,
    IOptions<QikLogOptions> options) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is not null || request.Headers.Contains("X-QikLog-API-Key"))
            return await base.SendAsync(request, cancellationToken);

        // No HttpContext (possible on an interactive circuit) means we cannot tell whether
        // the caller is signed in. Fail closed rather than assume anonymous and attach
        // the bootstrap tenant's hub key.
        if (httpContextAccessor.HttpContext is not { } httpContext)
            return await base.SendAsync(request, cancellationToken);

        var isAuthenticated = httpContext.User.Identity?.IsAuthenticated == true;
        var accessToken = isAuthenticated
            ? await httpContext.GetTokenAsync("access_token")
            : null;

        var credential = ApiCredentialPolicy.Choose(isAuthenticated, accessToken, options.Value.HubApiKey, isEmbed: false);
        switch (credential.Kind)
        {
            case ApiCredentialKind.AccessToken:
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credential.Value);
                break;
            case ApiCredentialKind.HubApiKey:
                request.Headers.Add("X-QikLog-API-Key", credential.Value);
                break;
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
