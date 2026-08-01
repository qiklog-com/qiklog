using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using QikLog.Infrastructure.Data;
using QikLog.Infrastructure.Tenants;
using Shouldly;
using Xunit;

namespace QikLog.Infrastructure.Tests;

public sealed class TenantProvisionerTests
{
    [Fact]
    public async Task EnsureTenant_claims_unclaimed_bootstrap_row_instead_of_inserting()
    {
        await using var db = CreateDb();
        var bootstrapId = Guid.Parse("11bb1044-1705-446b-b102-1504758c1804");
        db.Tenants.Add(new TenantEntity
        {
            Id = bootstrapId,
            Name = "QikLog Bootstrap",
            ZitadelOrgId = null,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            Plan = "free"
        });
        await db.SaveChangesAsync();

        var provisioner = new TenantProvisioner(db, NullLogger<TenantProvisioner>.Instance);
        var id = await provisioner.EnsureTenantAsync("org-123", "Jamey Org", CancellationToken.None);

        id.ShouldBe(bootstrapId);
        var row = await db.Tenants.SingleAsync();
        row.ZitadelOrgId.ShouldBe("org-123");
        row.Name.ShouldBe("Jamey Org");
    }

    [Fact]
    public async Task EnsureTenant_returns_existing_org_match()
    {
        await using var db = CreateDb();
        var existingId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity
        {
            Id = existingId,
            Name = "Already",
            ZitadelOrgId = "org-abc",
            CreatedAt = DateTimeOffset.UtcNow,
            Plan = "free"
        });
        await db.SaveChangesAsync();

        var provisioner = new TenantProvisioner(db, NullLogger<TenantProvisioner>.Instance);
        var id = await provisioner.EnsureTenantAsync("org-abc", "Other Name", CancellationToken.None);

        id.ShouldBe(existingId);
        (await db.Tenants.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task EnsureTenant_creates_new_when_no_unclaimed_bootstrap()
    {
        await using var db = CreateDb();
        db.Tenants.Add(new TenantEntity
        {
            Id = Guid.NewGuid(),
            Name = "Taken",
            ZitadelOrgId = "org-other",
            CreatedAt = DateTimeOffset.UtcNow,
            Plan = "free"
        });
        await db.SaveChangesAsync();

        var provisioner = new TenantProvisioner(db, NullLogger<TenantProvisioner>.Instance);
        var id = await provisioner.EnsureTenantAsync("org-new", "New Org", CancellationToken.None);

        id.ShouldNotBe(Guid.Empty);
        (await db.Tenants.CountAsync()).ShouldBe(2);
        (await db.Tenants.SingleAsync(t => t.Id == id)).ZitadelOrgId.ShouldBe("org-new");
    }

    private static QikLogDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<QikLogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new QikLogDbContext(options);
    }
}
