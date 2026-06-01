namespace QikLog.Infrastructure.Auth;

/// <summary>Per-request ingest auth state set by API middleware.</summary>
public interface IIngestContext
{
    Guid? ApiKeyId { get; set; }
}

public sealed class IngestContext : IIngestContext
{
    public Guid? ApiKeyId { get; set; }
}
