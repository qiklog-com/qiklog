using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QikLog.Core;
using QikLog.Infrastructure.Data;
using QikLog.Infrastructure.Tenants;

namespace QikLog.Infrastructure;

public interface ILogHistoryService
{
    bool IsEnabled { get; }

    Task<IReadOnlyList<LogEntry>> GetRecentBySourceAsync(
        string source,
        int limit,
        CancellationToken cancellationToken);
}

public sealed class NullLogHistoryService : ILogHistoryService
{
    public bool IsEnabled => false;

    public Task<IReadOnlyList<LogEntry>> GetRecentBySourceAsync(
        string source,
        int limit,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<LogEntry>>([]);
}

public sealed class EfLogHistoryService(QikLogDbContext db, ITenantContext tenantContext) : ILogHistoryService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public bool IsEnabled => true;

    public async Task<IReadOnlyList<LogEntry>> GetRecentBySourceAsync(
        string source,
        int limit,
        CancellationToken cancellationToken)
    {
        var capped = Math.Clamp(limit, 1, 500);
        var rows = await db.LogEntries
            .AsNoTracking()
            .Where(e => e.Source == source)
            .ForTenant(db, tenantContext.TenantId)
            .OrderByDescending(e => e.ReceivedAt)
            .Take(capped)
            .ToListAsync(cancellationToken);

        rows.Reverse();
        return rows.Select(ToLogEntry).ToList();
    }

    private static LogEntry ToLogEntry(LogEntryEntity entity)
    {
        IReadOnlyDictionary<string, string>? properties = null;
        if (!string.IsNullOrEmpty(entity.PropertiesJson))
        {
            properties = JsonSerializer.Deserialize<Dictionary<string, string>>(
                entity.PropertiesJson,
                JsonOptions);
        }

        return new LogEntry(
            entity.Source,
            entity.Level,
            entity.Message,
            entity.Timestamp,
            properties);
    }
}
