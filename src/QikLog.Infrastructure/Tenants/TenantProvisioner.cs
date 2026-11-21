using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QikLog.Infrastructure.Auth;
using QikLog.Infrastructure.Data;

namespace QikLog.Infrastructure.Tenants;

/// <summary>
/// Maps a Zitadel sign-in to a QikLog tenant. Each org gets exactly one tenant, except the
/// sign-up org (<see cref="QikLogAuthOptions.SignupOrgId"/>), where each user gets a
/// personal tenant. A sign-in without an org id is refused so no unowned tenant rows exist.
/// </summary>
public sealed class TenantProvisioner(
    QikLogDbContext db,
    IOptions<QikLogAuthOptions> authOptions,
    ILogger<TenantProvisioner> log)
{
    public Task<Guid> EnsureTenantAsync(string? zitadelOrgId, string displayName, CancellationToken cancellationToken) =>
        EnsureTenantAsync(zitadelOrgId, zitadelUserId: null, displayName, cancellationToken);

    public async Task<Guid> EnsureTenantAsync(
        string? zitadelOrgId,
        string? zitadelUserId,
        string displayName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(zitadelOrgId))
            throw new ArgumentException("A Zitadel organization id is required to provision a tenant.", nameof(zitadelOrgId));

        var orgId = zitadelOrgId.Trim();

        // Defense in depth: callers check first, but never create a tenant for the shared
        // default org, whose members would all land in one tenant.
        if (authOptions.Value.IsSharedDefaultOrg(orgId))
            throw new InvalidOperationException("Refusing to provision a tenant for the shared Zitadel default organization.");

        if (authOptions.Value.IsSignupOrg(orgId))
            return await EnsurePersonalTenantAsync(orgId, zitadelUserId, displayName, cancellationToken);

        var existing = await db.Tenants
            .FirstOrDefaultAsync(t => t.ZitadelOrgId == orgId && t.ZitadelUserId == null, cancellationToken);
        if (existing is not null)
            return existing.Id;

        // The invite-beta bootstrap row (ZitadelOrgId = NULL) holds the hub API key and all
        // pre-OIDC logs. Only the explicitly configured org may claim it; anyone else
        // claiming it would inherit another person's keys and history.
        var bootstrapOrgId = authOptions.Value.BootstrapOrgId?.Trim();
        if (!string.IsNullOrEmpty(bootstrapOrgId) && string.Equals(orgId, bootstrapOrgId, StringComparison.Ordinal))
        {
            var unclaimed = await db.Tenants
                .Where(t => t.ZitadelOrgId == null && t.ZitadelUserId == null)
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

    private async Task<Guid> EnsurePersonalTenantAsync(
        string orgId,
        string? zitadelUserId,
        string displayName,
        CancellationToken cancellationToken)
    {
        // Everyone in the sign-up org shares that org; without a user id there is no
        // boundary between them. Refuse rather than guess.
        if (string.IsNullOrWhiteSpace(zitadelUserId))
            throw new ArgumentException("A Zitadel user id is required to provision a personal tenant.", nameof(zitadelUserId));

        var userId = zitadelUserId.Trim();
        var existing = await db.Tenants
            .FirstOrDefaultAsync(t => t.ZitadelUserId == userId, cancellationToken);
        if (existing is not null)
            return existing.Id;

        var tenant = new TenantEntity
        {
            Id = Guid.NewGuid(),
            Name = string.IsNullOrWhiteSpace(displayName) ? "Workspace" : displayName.Trim(),
            ZitadelOrgId = orgId,
            ZitadelUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
            Plan = "free"
        };

        db.Tenants.Add(tenant);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two first sign-ins raced (two tabs). The other one created it; use that.
            db.Entry(tenant).State = EntityState.Detached;
            var created = await db.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.ZitadelUserId == userId, cancellationToken);
            if (created is null)
                throw;
            return created.Id;
        }

        log.LogInformation("Provisioned personal tenant {TenantId} for user {UserId} in sign-up org", tenant.Id, userId);
        return tenant.Id;
    }
}
