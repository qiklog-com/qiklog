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
        }

        var tenant = new TenantEntity
        {
            Id = Guid.NewGuid(),
            Name = displayName.Trim(),
            ZitadelOrgId = zitadelOrgId,
            CreatedAt = DateTimeOffset.UtcNow,
            Plan = "free"
        };

        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(cancellationToken);
        log.LogInformation("Provisioned tenant {TenantId} for org {OrgId}", tenant.Id, zitadelOrgId);
        return tenant.Id;
    }
}
