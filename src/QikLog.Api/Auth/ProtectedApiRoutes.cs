namespace QikLog.Api.Auth;

[Flags]
internal enum AuthMode
{
    None = 0,
    ApiKey = 1,
    Jwt = 2,
    JwtOrApiKey = Jwt | ApiKey
}

internal static class ProtectedApiRoutes
{
    public static bool TryGetAuthMode(PathString path, string method, out AuthMode mode)
    {
        mode = AuthMode.None;

        if (path.StartsWithSegments("/v1/logs", StringComparison.OrdinalIgnoreCase)
            && HttpMethods.IsPost(method))
        {
            mode = AuthMode.ApiKey;
            return true;
        }

        if (path.StartsWithSegments("/v1/sources", StringComparison.OrdinalIgnoreCase)
            && path.Value?.EndsWith("/logs", StringComparison.OrdinalIgnoreCase) == true
            && HttpMethods.IsGet(method))
        {
            mode = AuthMode.JwtOrApiKey;
            return true;
        }

        if (path.StartsWithSegments("/v1/keys", StringComparison.OrdinalIgnoreCase)
            || (path.StartsWithSegments("/v1/sources", StringComparison.OrdinalIgnoreCase)
                && HttpMethods.IsGet(method)))
        {
            mode = AuthMode.Jwt;
            return true;
        }

        if (path.StartsWithSegments("/v1/billing", StringComparison.OrdinalIgnoreCase))
        {
            mode = AuthMode.Jwt;
            return true;
        }

        if (path.StartsWithSegments("/v1/dev/keys", StringComparison.OrdinalIgnoreCase)
            && HttpMethods.IsPost(method))
        {
            mode = AuthMode.Jwt;
            return true;
        }

        return false;
    }

    public static bool IsPublic(PathString path) =>
        path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/healthz", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/metrics", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/scalar", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase);
}
