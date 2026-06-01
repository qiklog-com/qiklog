using Microsoft.EntityFrameworkCore;
using QikLog.Core.Management;
using QikLog.Infrastructure.Data;

namespace QikLog.Infrastructure.Sources;

public sealed class SourceCatalogService(QikLogDbContext db) : ISourceCatalog
{
    public async Task<IReadOnlyList<SourceSummary>> ListAsync(CancellationToken cancellationToken)
    {
        var entries = await db.LogEntries.AsNoTracking().ToListAsync(cancellationToken);
        return entries
            .GroupBy(e => e.Source)
            .Select(g => new SourceSummary(
                g.Key,
                g.LongCount(),
                g.Max(e => e.ReceivedAt)))
            .OrderByDescending(s => s.LastReceivedAt)
            .ToList();
    }
}
