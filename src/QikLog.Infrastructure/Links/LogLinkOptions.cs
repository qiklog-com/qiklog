namespace QikLog.Infrastructure.Links;

/// <summary>Limits for anonymous log links. Bound from <c>QikLog:Links</c>.</summary>
public sealed class LogLinkOptions
{
    public const string SectionName = "QikLog:Links";

    public bool Enabled { get; set; } = true;

    public int LifetimeHours { get; set; } = 24;

    /// <summary>Lines one link accepts before sending returns 402.</summary>
    public int MaxLines { get; set; } = 500;

    /// <summary>Characters in one line.</summary>
    public int MaxLineLength { get; set; } = 2048;

    /// <summary>Lines in one request (a piped multi-line body).</summary>
    public int MaxLinesPerRequest { get; set; } = 50;

    /// <summary>New links per client address per clock hour. Zero or less turns the limit off.</summary>
    public int CreatesPerIpPerHour { get; set; } = 20;

    /// <summary>
    /// Origin shown in link URLs, e.g. <c>https://qiklog.com</c>. When blank the API uses
    /// the origin the request came in on.
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>
    /// Web app origin, e.g. <c>https://app.qiklog.com</c>. When set, a browser that opens
    /// a link on the API is redirected to the live tail there.
    /// </summary>
    public string? WebBaseUrl { get; set; }
}
