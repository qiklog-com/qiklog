namespace QikLog.Infrastructure.Billing;

public sealed class UsageLimitOptions
{
    public const string SectionName = "QikLog:Usage";

    /// <summary>Max ingest requests per calendar month on the free plan.</summary>
    public int FreeIngestPerMonth { get; set; } = 10_000;

    public int ProIngestPerMonth { get; set; } = 500_000;
}
