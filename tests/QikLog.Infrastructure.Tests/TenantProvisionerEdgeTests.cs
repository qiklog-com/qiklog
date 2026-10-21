using Microsoft.EntityFrameworkCore;
using QikLog.Infrastructure.Data;
using Shouldly;
using Xunit;

namespace QikLog.Infrastructure.Tests;

public sealed class TenantProvisionerEdgeTests
{
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

        var provisioner = TenantProvisionerTests.CreateProvisioner(db, bootstrapOrgId: "org-keep");
        await provisioner.EnsureTenantAsync("org-keep", "   ", CancellationToken.None);

        (await db.Tenants.SingleAsync()).Name.ShouldBe("Bootstrap Keep");
        (await db.Tenants.SingleAsync()).ZitadelOrgId.ShouldBe("org-keep");
    }

    [Fact]
    public async Task EnsureTenant_bootstrap_org_claims_oldest_unclaimed()
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

        var provisioner = TenantProvisionerTests.CreateProvisioner(db, bootstrapOrgId: "org-old");
        var id = await provisioner.EnsureTenantAsync("org-old", "Claimed", CancellationToken.None);
        id.ShouldBe(older);
        (await db.Tenants.SingleAsync(t => t.Id == newer)).ZitadelOrgId.ShouldBeNull();
    }

    [Fact]
    public async Task EnsureTenant_trims_org_id_and_display_name()
    {
        await using var db = CreateDb();
        var provisioner = TenantProvisionerTests.CreateProvisioner(db, bootstrapOrgId: null);
        var id = await provisioner.EnsureTenantAsync("  org-trim  ", "  Trimmed Co  ", CancellationToken.None);
        var row = await db.Tenants.SingleAsync(t => t.Id == id);
        row.ZitadelOrgId.ShouldBe("org-trim");
        row.Name.ShouldBe("Trimmed Co");
    }

    [Fact]
    public async Task EnsureTenant_lookup_uses_trimmed_org_id()
    {
        await using var db = CreateDb();
        var provisioner = TenantProvisionerTests.CreateProvisioner(db, bootstrapOrgId: null);
        var first = await provisioner.EnsureTenantAsync("org-pad", "Pad", CancellationToken.None);
        var second = await provisioner.EnsureTenantAsync(" org-pad ", "Pad", CancellationToken.None);
        second.ShouldBe(first);
        (await db.Tenants.CountAsync()).ShouldBe(1);
    }

    private static QikLogDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<QikLogDbContext>()
            .UseInMemoryDatabase($"provisioner-edge-{Guid.NewGuid():N}")
            .Options;
        return new QikLogDbContext(options);
    }
}
