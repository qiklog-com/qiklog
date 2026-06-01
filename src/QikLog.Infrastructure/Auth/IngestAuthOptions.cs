namespace QikLog.Infrastructure.Auth;

/// <summary>Configuration for ingest API key auth and rate limits.</summary>
public sealed class IngestAuthOptions
{
    public const string SectionName = "QikLog:Ingest";

    /// <summary>When true, <c>POST /v1/logs</c> requires a valid API key.</summary>
    public bool RequireApiKey { get; set; }

    /// <summary>Max ingest requests per API key per minute.</summary>
    public int RateLimitPerMinute { get; set; } = 120;
}
