using Microsoft.Extensions.Options;
using QikLog.Api.Auth;
using QikLog.Api.Hubs;
using QikLog.Infrastructure;
using QikLog.Infrastructure.Auth;
using QikLog.Infrastructure.Tenants;

namespace QikLog.Api.Middleware;

/// <summary>Enforces JWT and/or API-key authentication and sets <see cref="ITenantContext"/>.</summary>
public sealed class TenantAuthMiddleware(
    RequestDelegate next,
    TenantAuthenticationService authentication,
    IOptions<AuthEnforcementOptions> enforcementOptions,
    IOptions<ManagementOptions> managementOptions,
    ILogger<TenantAuthMiddleware> log)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (ProtectedApiRoutes.IsPublic(context.Request.Path))
        {
            await next(context);
            return;
        }

        var store = context.RequestServices.GetRequiredService<ILogEntryStore>();
        if (!store.IsEnabled || !enforcementOptions.Value.Enabled)
        {
            await next(context);
            return;
        }

        if (!ProtectedApiRoutes.TryGetAuthMode(
                context.Request.Path,
                context.Request.Method,
                out var authMode))
        {
            await next(context);
            return;
        }

        if (authMode == AuthMode.Jwt && !managementOptions.Value.Enabled
            && context.Request.Path.StartsWithSegments("/v1/keys", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        if (authMode == AuthMode.Jwt && !managementOptions.Value.Enabled
            && context.Request.Path.StartsWithSegments("/v1/sources", StringComparison.OrdinalIgnoreCase)
            && context.Request.Path.Value?.EndsWith("/logs", StringComparison.OrdinalIgnoreCase) != true)
        {
            await next(context);
            return;
        }

        var applyRateLimit = context.Request.Path.StartsWithSegments("/v1/logs", StringComparison.OrdinalIgnoreCase);
        var (success, failure) = await authentication.AuthenticateAsync(
            context,
            authMode,
            applyRateLimit,
            context.RequestAborted);

        if (success is null)
        {
            await WriteFailureAsync(context, authMode, failure);
            return;
        }

        var tenantContext = context.RequestServices.GetRequiredService<ITenantContext>();
        tenantContext.TenantId = success.TenantId;

        if (success.ApiKeyId is Guid apiKeyId)
            context.RequestServices.GetRequiredService<IIngestContext>().ApiKeyId = apiKeyId;

        if (context.Request.Path.StartsWithSegments("/hubs/logs", StringComparison.OrdinalIgnoreCase))
            context.Items[LogHub.TenantIdItemKey] = success.TenantId;

        await next(context);
    }

    private async Task WriteFailureAsync(HttpContext context, AuthMode mode, TenantAuthFailure failure)
    {
        switch (failure)
        {
            case TenantAuthFailure.RateLimited:
                log.LogWarning("Ingest rate limit exceeded for {Path}", context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsJsonAsync(new { error = "rate limit exceeded" });
                break;
            case TenantAuthFailure.MissingCredentials:
                log.LogWarning("Auth missing for {Path}", context.Request.Path);
                await WriteErrorAsync(context, StatusCodes.Status401Unauthorized,
                    mode.HasFlag(AuthMode.ApiKey) && !mode.HasFlag(AuthMode.Jwt)
                        ? "missing API key (header X-QikLog-API-Key)"
                        : "authentication required");
                break;
            case TenantAuthFailure.InvalidCredentials:
                log.LogWarning("Auth invalid for {Path}", context.Request.Path);
                await WriteErrorAsync(context, StatusCodes.Status403Forbidden, "invalid or revoked API key");
                break;
            case TenantAuthFailure.TenantNotFound:
                log.LogWarning("Auth tenant not found for {Path}", context.Request.Path);
                await WriteErrorAsync(context, StatusCodes.Status403Forbidden,
                    "tenant not found or not provisioned");
                break;
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new { error = message });
    }
}
