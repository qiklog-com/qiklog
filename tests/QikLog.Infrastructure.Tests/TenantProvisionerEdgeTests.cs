using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using QikLog.Infrastructure.Data;
using QikLog.Infrastructure.Tenants;
using Shouldly;
using Xunit;

namespace QikLog.Infrastructure.Tests;

public sealed class TenantProvisionerEdgeTests
{
    [Fact]
    public async Task EnsureTenant_null_org_creates_tenant_with_null_ZitadelOrgId()
    {
        await using var db = CreateDb();
        var provisioner = new TenantProvisioner(db, NullLogger<TenantProvisioner>.Instance);

        var id = await provisioner.EnsureTenantAsync(null, "Solo", CancellationToken.None);

        var row = await db.Tenants.SingleAsync(t => t.Id == id);
        row.ZitadelOrgId.ShouldBeNull();
        row.Name.ShouldBe("Solo");
    }

    [Fact]
    public async Task EnsureTenant_claim_keeps_name_when_displayName_whitespace()
    {
        await using var db = CreateDb();
        var bootstrapId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity
        {
            Id = bootstrapId,
            Name = "Bootstrap Keep",
            ZitadelOrgId = null,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            Plan = "free"
        });
        await db.SaveChangesAsync();

        var provisioner = new TenantProvisioner(db, NullLogger<TenantProvisioner>.Instance);
        await provisioner.EnsureTenantAsync("org-keep", "   ", CancellationToken.None);

        (await db.Tenants.SingleAsync()).Name.ShouldBe("Bootstrap Keep");
        (await db.Tenants.SingleAsync()).ZitadelOrgId.ShouldBe("org-keep");
    }

    [Fact]
    public async Task EnsureTenant_claims_oldest_unclaimed_bootstrap()
    {
        await using var db = CreateDb();
        var older = Guid.NewGuid();
        var newer = Guid.NewGuid();
        db.Tenants.AddRange(
            new TenantEntity
            {
                Id = older,
                Name = "Older",
                ZitadelOrgId = null,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
                Plan = "free"
            },
            new TenantEntity
            {
                Id = newer,
                Name = "Newer",
                ZitadelOrgId = null,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
                Plan = "free"
            });
        await db.SaveChangesAsync();

        var provisioner = new TenantProvisioner(db, NullLogger<TenantProvisioner>.Instance);
        var id = await provisioner.EnsureTenantAsync("org-old", "Claimed", CancellationToken.None);
        id.ShouldBe(older);
        (await db.Tenants.SingleAsync(t => t.Id == newer)).ZitadelOrgId.ShouldBeNull();
    }

    [Fact]
    public async Task EnsureTenant_trims_org_id_and_display_name()
    {
        await using var db = CreateDb();
        var provisioner = new TenantProvisioner(db, NullLogger<TenantProvisioner>.Instance);
        var id = await provisioner.EnsureTenantAsync("  org-trim  ", "  Trimmed Co  ", CancellationToken.None);
        var row = await db.Tenants.SingleAsync(t => t.Id == id);
        row.ZitadelOrgId.ShouldBe("org-trim");
        row.Name.ShouldBe("Trimmed Co");
    }

    private static QikLogDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<QikLogDbContext>()
            .UseInMemoryDatabase($"provisioner-edge-{Guid.NewGuid():N}")
            .Options;
        return new QikLogDbContext(options);
    }
}
