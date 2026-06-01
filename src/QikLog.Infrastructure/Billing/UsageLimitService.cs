using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QikLog.Infrastructure.Data;
using QikLog.Infrastructure.Tenants;

namespace QikLog.Infrastructure.Billing;

public interface IUsageLimitService
{
    Task<UsageCheckResult> CheckIngestAllowedAsync(CancellationToken cancellationToken);
}

public sealed record UsageCheckResult(bool Allowed, string? Reason, long Count, long Limit);

public sealed class UsageLimitService(
    QikLogDbContext db,
    ITenantContext tenantContext,
    IOptions<UsageLimitOptions> options,
    ILogger<UsageLimitService> log) : IUsageLimitService
{
    public async Task<UsageCheckResult> CheckIngestAllowedAsync(CancellationToken cancellationToken)
    {
        var limit = options.Value.FreeIngestPerMonth;
        if (tenantContext.TenantId is Guid tenantId)
        {
            var plan = await db.Tenants
                .AsNoTracking()
                .Where(t => t.Id == tenantId)
                .Select(t => t.Plan)
                .FirstOrDefaultAsync(cancellationToken);
            if (string.Equals(plan, "pro", StringComparison.OrdinalIgnoreCase))
                limit = options.Value.ProIngestPerMonth;
        }

        var monthStart = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var entries = db.LogEntries.Where(e => e.ReceivedAt >= monthStart);
        entries = entries.ForTenant(db, tenantContext.TenantId);
        var count = await entries.LongCountAsync(cancellationToken);

        if (count >= limit)
        {
            log.LogWarning(
                "Monthly ingest limit exceeded for tenant {TenantId}: {Count}/{Limit}",
                tenantContext.TenantId,
                count,
                limit);
            return new UsageCheckResult(false, "monthly ingest limit exceeded — upgrade to Pro", count, limit);
        }

        log.LogDebug(
            "Ingest allowed for tenant {TenantId}: {Count}/{Limit}",
            tenantContext.TenantId,
            count,
            limit);
        return new UsageCheckResult(true, null, count, limit);
    }
}

public sealed class NullUsageLimitService : IUsageLimitService
{
    public Task<UsageCheckResult> CheckIngestAllowedAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new UsageCheckResult(true, null, 0, long.MaxValue));
}
