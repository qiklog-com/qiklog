using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QikLog.Infrastructure.Auth;
using QikLog.Infrastructure.Data;

namespace QikLog.Infrastructure.Tenants;

public enum TenantResolutionStatus
{
    Unauthenticated,
    TenantNotFound,
    Success
}

public sealed record TenantResolution(TenantResolutionStatus Status, Guid? TenantId = null);

/// <summary>Resolves a tenant id from an authenticated JWT principal.</summary>
public sealed class TenantResolver(
    QikLogDbContext db,
    TenantProvisioner provisioner,
    IOptions<QikLogAuthOptions> authOptions)
{
    public async Task<TenantResolution> ResolveFromPrincipalAsync(
        ClaimsPrincipal? principal,
        CancellationToken cancellationToken)
    {
        if (principal?.Identity?.IsAuthenticated != true)
            return new TenantResolution(TenantResolutionStatus.Unauthenticated);

        var tenantIdClaim = principal.FindFirst("tenant_id")?.Value;
        if (Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            var exists = await db.Tenants.AsNoTracking()
                .AnyAsync(t => t.Id == tenantId, cancellationToken);
            return exists
                ? new TenantResolution(TenantResolutionStatus.Success, tenantId)
                : new TenantResolution(TenantResolutionStatus.TenantNotFound);
        }

        var orgId = principal.FindFirst(authOptions.Value.OrganizationClaim)?.Value;
        if (string.IsNullOrWhiteSpace(orgId))
            return new TenantResolution(TenantResolutionStatus.TenantNotFound);

        var name = principal.FindFirst("name")?.Value
            ?? principal.FindFirst(ClaimTypes.Email)?.Value
            ?? "Tenant";
        var provisionedId = await provisioner.EnsureTenantAsync(orgId, name, cancellationToken);
        return new TenantResolution(TenantResolutionStatus.Success, provisionedId);
    }
}
