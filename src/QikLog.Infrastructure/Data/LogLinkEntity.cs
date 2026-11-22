namespace QikLog.Infrastructure.Data;

/// <summary>
/// An anonymous log link (<c>/l/{id}</c>). It belongs to no tenant; its lines are
/// <see cref="LogEntryEntity"/> rows with no API key under the source <c>link-{id}</c>.
/// </summary>
public sealed class LogLinkEntity
{
    /// <summary>The random id from the URL. See <c>QikLog.Core.Links.LogLinkId</c>.</summary>
    public required string Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>After this moment the link refuses lines and is deleted on the next sweep.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Lines accepted so far. Sending stops at the configured cap.</summary>
    public int LineCount { get; set; }
}
