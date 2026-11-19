namespace QikLog.Web.Demo;

/// <summary>Server-only settings for the try-it-now page. Bound from <c>QikLog:Demo</c>.</summary>
public sealed class DemoOptions
{
    /// <summary>
    /// Ingest and tail credential for the playground. Never render this value.
    /// When blank, <see cref="DemoCredentials"/> falls back to <c>QikLog:HubApiKey</c>.
    /// </summary>
    public string? ApiKey { get; set; }
}
