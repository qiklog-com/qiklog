using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QikLog.Infrastructure.Data;

namespace QikLog.Infrastructure.Tenants;

public sealed class TenantProvisioner(QikLogDbContext db, ILogger<TenantProvisioner> log)
{
    public async Task<Guid> EnsureTenantAsync(string? zitadelOrgId, string displayName, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(zitadelOrgId))
        {
            var existing = await db.Tenants
                .FirstOrDefaultAsync(t => t.ZitadelOrgId == zitadelOrgId, cancellationToken);
            if (existing is not null)
                return existing.Id;

            // Invite-beta bootstrap rows were created with ZitadelOrgId = NULL and hold the
            // hub API key. First real login must claim that row instead of inserting a second
            // tenant — otherwise Manage/keys and live tail disagree about which tenant is live.
            var unclaimed = await db.Tenants
                .Where(t => t.ZitadelOrgId == null)
                .OrderBy(t => t.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (unclaimed is not null)
            {
                unclaimed.ZitadelOrgId = zitadelOrgId.Trim();
                if (!string.IsNullOrWhiteSpace(displayName))
                    unclaimed.Name = displayName.Trim();

                await db.SaveChangesAsync(cancellationToken);
                log.LogInformation(
                    "Claimed bootstrap tenant {TenantId} for org {OrgId}",
                    unclaimed.Id,
                    zitadelOrgId);
                return unclaimed.Id;
            }
        }

        var tenant = new TenantEntity
        {
            Id = Guid.NewGuid(),
            Name = displayName.Trim(),
            ZitadelOrgId = string.IsNullOrWhiteSpace(zitadelOrgId) ? null : zitadelOrgId.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            Plan = "free"
        };

        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(cancellationToken);
        log.LogInformation("Provisioned tenant {TenantId} for org {OrgId}", tenant.Id, zitadelOrgId);
        return tenant.Id;
    }
}
