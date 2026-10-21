using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QikLog.Infrastructure.Auth;
using QikLog.Infrastructure.Data;

namespace QikLog.Infrastructure.Tenants;

/// <summary>
/// Maps a Zitadel organization to a QikLog tenant on sign-in. Each org gets exactly one
/// tenant. A sign-in without an org id is refused so no unowned tenant rows exist.
/// </summary>
public sealed class TenantProvisioner(
    QikLogDbContext db,
    IOptions<QikLogAuthOptions> authOptions,
    ILogger<TenantProvisioner> log)
{
    public async Task<Guid> EnsureTenantAsync(string? zitadelOrgId, string displayName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(zitadelOrgId))
            throw new ArgumentException("A Zitadel organization id is required to provision a tenant.", nameof(zitadelOrgId));

        var orgId = zitadelOrgId.Trim();

        var existing = await db.Tenants
            .FirstOrDefaultAsync(t => t.ZitadelOrgId == orgId, cancellationToken);
        if (existing is not null)
            return existing.Id;

        // The invite-beta bootstrap row (ZitadelOrgId = NULL) holds the hub API key and all
        // pre-OIDC logs. Only the explicitly configured org may claim it; anyone else
        // claiming it would inherit another person's keys and history.
        var bootstrapOrgId = authOptions.Value.BootstrapOrgId?.Trim();
        if (!string.IsNullOrEmpty(bootstrapOrgId) && string.Equals(orgId, bootstrapOrgId, StringComparison.Ordinal))
        {
            var unclaimed = await db.Tenants
                .Where(t => t.ZitadelOrgId == null)
                .OrderBy(t => t.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (unclaimed is not null)
            {
                unclaimed.ZitadelOrgId = orgId;
                if (!string.IsNullOrWhiteSpace(displayName))
                    unclaimed.Name = displayName.Trim();

                await db.SaveChangesAsync(cancellationToken);
                log.LogInformation(
                    "Claimed bootstrap tenant {TenantId} for org {OrgId}",
                    unclaimed.Id,
                    orgId);
                return unclaimed.Id;
            }
        }

        var tenant = new TenantEntity
        {
            Id = Guid.NewGuid(),
            Name = string.IsNullOrWhiteSpace(displayName) ? "Tenant" : displayName.Trim(),
            ZitadelOrgId = orgId,
            CreatedAt = DateTimeOffset.UtcNow,
            Plan = "free"
        };

        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(cancellationToken);
        log.LogInformation("Provisioned tenant {TenantId} for org {OrgId}", tenant.Id, orgId);
        return tenant.Id;
    }
}
