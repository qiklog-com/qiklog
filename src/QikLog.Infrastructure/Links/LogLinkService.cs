using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QikLog.Core;
using QikLog.Core.Links;
using QikLog.Infrastructure.Data;
using LogLevel = QikLog.Core.LogLevel;

namespace QikLog.Infrastructure.Links;

public enum LogLinkStatus
{
    Ok = 0,
    NotFound = 1,
    Expired = 2,
    Full = 3
}

public sealed record LogLink(string Id, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, int LineCount);

/// <param name="Entries">Lines that were stored, in order. Empty unless <see cref="Status"/> is Ok.</param>
/// <param name="Remaining">Lines the link can still take.</param>
public sealed record LogLinkAppendResult(LogLinkStatus Status, IReadOnlyList<LogEntry> Entries, int Remaining);

public sealed record LogLinkReadResult(LogLinkStatus Status, IReadOnlyList<LogEntry> Entries);

/// <summary>Anonymous log links: create one, append lines, read them back.</summary>
public interface ILogLinkService
{
    /// <summary>False when no database is configured; the endpoints then answer 503.</summary>
    bool IsEnabled { get; }

    Task<LogLink> CreateAsync(CancellationToken cancellationToken);

    Task<LogLinkAppendResult> AppendAsync(
        string id,
        IReadOnlyList<LinkLine> lines,
        CancellationToken cancellationToken);

    /// <summary>Newest <paramref name="limit"/> lines, oldest first. Works on a full link.</summary>
    Task<LogLinkReadResult> ReadAsync(string id, int limit, CancellationToken cancellationToken);

    /// <summary>Deletes up to <paramref name="maxLinks"/> expired links and their lines.</summary>
    Task<int> PurgeExpiredAsync(int maxLinks, CancellationToken cancellationToken);
}

public sealed class NullLogLinkService : ILogLinkService
{
    public bool IsEnabled => false;

    public Task<LogLink> CreateAsync(CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Log links need a database.");

    public Task<LogLinkAppendResult> AppendAsync(
        string id,
        IReadOnlyList<LinkLine> lines,
        CancellationToken cancellationToken) =>
        Task.FromResult(new LogLinkAppendResult(LogLinkStatus.NotFound, [], 0));

    public Task<LogLinkReadResult> ReadAsync(string id, int limit, CancellationToken cancellationToken) =>
        Task.FromResult(new LogLinkReadResult(LogLinkStatus.NotFound, []));

    public Task<int> PurgeExpiredAsync(int maxLinks, CancellationToken cancellationToken) =>
        Task.FromResult(0);
}

public sealed class EfLogLinkService(
    QikLogDbContext db,
    IOptions<LogLinkOptions> options,
    TimeProvider time,
    ILogger<EfLogLinkService> log) : ILogLinkService
{
    private const int MaxReadLimit = 500;

    public bool IsEnabled => true;

    public async Task<LogLink> CreateAsync(CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        var entity = new LogLinkEntity
        {
            Id = LogLinkId.New(),
            CreatedAt = now,
            ExpiresAt = now.AddHours(Math.Max(1, options.Value.LifetimeHours)),
            LineCount = 0
        };

        db.LogLinks.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        // The id is the link's only secret, so it stays out of the logs.
        log.LogInformation("Created a log link expiring {ExpiresAt}", entity.ExpiresAt);
        return ToLink(entity);
    }

    public async Task<LogLinkAppendResult> AppendAsync(
        string id,
        IReadOnlyList<LinkLine> lines,
        CancellationToken cancellationToken)
    {
        if (!LogLinkId.IsValid(id))
            return new LogLinkAppendResult(LogLinkStatus.NotFound, [], 0);

        var link = await db.LogLinks.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (link is null)
            return new LogLinkAppendResult(LogLinkStatus.NotFound, [], 0);

        var now = time.GetUtcNow();
        if (link.ExpiresAt <= now)
            return new LogLinkAppendResult(LogLinkStatus.Expired, [], 0);

        var room = options.Value.MaxLines - link.LineCount;
        if (room <= 0)
            return new LogLinkAppendResult(LogLinkStatus.Full, [], 0);

        var source = LogLinkId.SourceFor(id);
        var entries = new List<LogEntry>(Math.Min(room, lines.Count));
        foreach (var line in lines.Take(room))
        {
            entries.Add(new LogEntry(source, line.Level, line.Message, now));
            db.LogEntries.Add(new LogEntryEntity
            {
                Source = source,
                Level = line.Level,
                Message = line.Message,
                Timestamp = now,
                ReceivedAt = now,
                PropertiesJson = null,
                ApiKeyId = null
            });
        }

        link.LineCount += entries.Count;
        await db.SaveChangesAsync(cancellationToken);

        return new LogLinkAppendResult(LogLinkStatus.Ok, entries, options.Value.MaxLines - link.LineCount);
    }

    public async Task<LogLinkReadResult> ReadAsync(string id, int limit, CancellationToken cancellationToken)
    {
        if (!LogLinkId.IsValid(id))
            return new LogLinkReadResult(LogLinkStatus.NotFound, []);

        var link = await db.LogLinks.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (link is null)
            return new LogLinkReadResult(LogLinkStatus.NotFound, []);

        if (link.ExpiresAt <= time.GetUtcNow())
            return new LogLinkReadResult(LogLinkStatus.Expired, []);

        var source = LogLinkId.SourceFor(id);
        var rows = await db.LogEntries
            .AsNoTracking()
            .Where(e => e.Source == source && e.ApiKeyId == null)
            .OrderByDescending(e => e.ReceivedAt)
            .ThenByDescending(e => e.Id)
            .Take(Math.Clamp(limit, 1, MaxReadLimit))
            .ToListAsync(cancellationToken);

        rows.Reverse();
        return new LogLinkReadResult(
            LogLinkStatus.Ok,
            rows.Select(r => new LogEntry(r.Source, r.Level, r.Message, r.Timestamp)).ToList());
    }

    public async Task<int> PurgeExpiredAsync(int maxLinks, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        var expired = await db.LogLinks
            .Where(l => l.ExpiresAt <= now)
            .OrderBy(l => l.ExpiresAt)
            .Take(Math.Max(1, maxLinks))
            .ToListAsync(cancellationToken);

        if (expired.Count == 0)
            return 0;

        var sources = expired.Select(l => LogLinkId.SourceFor(l.Id)).ToList();
        var lines = await db.LogEntries
            .Where(e => e.ApiKeyId == null && sources.Contains(e.Source))
            .ToListAsync(cancellationToken);

        db.LogEntries.RemoveRange(lines);
        db.LogLinks.RemoveRange(expired);
        await db.SaveChangesAsync(cancellationToken);

        log.LogInformation(
            "Purged {LinkCount} expired log links and {LineCount} lines",
            expired.Count,
            lines.Count);
        return expired.Count;
    }

    private static LogLink ToLink(LogLinkEntity entity) =>
        new(entity.Id, entity.CreatedAt, entity.ExpiresAt, entity.LineCount);
}
