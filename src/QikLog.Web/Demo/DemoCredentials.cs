using Microsoft.Extensions.Logging;

namespace QikLog.Web.Demo;

/// <summary>Resolves the server-side credential for the try-it-now page.</summary>
public static class DemoCredentials
{
    /// <summary>Startup warning when the demo key is unset and the hub key is used instead.</summary>
    public const string FallbackWarning =
        "QikLog:Demo:ApiKey is unset; the try-it-now page is using QikLog:HubApiKey.";

    /// <summary>
    /// Demo key when set, otherwise the hub key. The result stays on the server.
    /// </summary>
    public static string? Resolve(QikLogOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Demo.ApiKey))
            return options.Demo.ApiKey.Trim();

        if (!string.IsNullOrWhiteSpace(options.HubApiKey))
            return options.HubApiKey.Trim();

        return null;
    }

    /// <summary>Logs <see cref="FallbackWarning"/> once when the hub key is the fallback.</summary>
    public static void LogFallbackIfNeeded(QikLogOptions options, ILogger logger)
    {
        if (!string.IsNullOrWhiteSpace(options.Demo.ApiKey))
            return;

        if (string.IsNullOrWhiteSpace(options.HubApiKey))
            return;

        logger.LogWarning(FallbackWarning);
    }
}
