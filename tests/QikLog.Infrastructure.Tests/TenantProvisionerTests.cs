using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QikLog.Infrastructure.Auth;
using QikLog.Infrastructure.Data;
using QikLog.Infrastructure.Tenants;
using Shouldly;
using Xunit;

namespace QikLog.Infrastructure.Tests;

/// <summary>
/// Self-serve signup: every new Zitadel org gets its own tenant. The invite-beta
/// bootstrap tenant (hub key + historical logs) is claimable only by the org
/// explicitly named in <c>QikLog:Auth:BootstrapOrgId</c>.
/// </summary>
public sealed class TenantProvisionerTests
{
    private static readonly Guid BootstrapId = Guid.Parse("11bb1044-1705-446b-b102-1504758c1804");

    [Fact]
    public async Task Given_unclaimed_bootstrap_tenant_When_a_new_org_signs_in_Then_it_gets_its_own_tenant()
    {
        // Given: the bootstrap tenant exists with no org, and a bootstrap org is configured
        await using var db = CreateDb();
        SeedUnclaimedBootstrap(db);
        var provisioner = CreateProvisioner(db, bootstrapOrgId: "org-jamey");

        // When: a stranger's org signs in for the first time
        var id = await provisioner.EnsureTenantAsync("org-stranger", "Stranger", CancellationToken.None);

        // Then: they get a fresh tenant and the bootstrap row stays unclaimed
        id.ShouldNotBe(BootstrapId);
        (await db.Tenants.SingleAsync(t => t.Id == id)).ZitadelOrgId.ShouldBe("org-stranger");
        (await db.Tenants.SingleAsync(t => t.Id == BootstrapId)).ZitadelOrgId.ShouldBeNull();
    }

    [Fact]
    public async Task Given_no_bootstrap_org_configured_When_any_org_signs_in_Then_bootstrap_is_never_claimed()
    {
        // Given: an unclaimed bootstrap tenant and no BootstrapOrgId setting
        await using var db = CreateDb();
        SeedUnclaimedBootstrap(db);
        var provisioner = CreateProvisioner(db, bootstrapOrgId: null);

        // When: the first org ever signs in
        var id = await provisioner.EnsureTenantAsync("org-first", "First", CancellationToken.None);

        // Then: fail safe, a new tenant rather than inheriting the hub key and history
        id.ShouldNotBe(BootstrapId);
        (await db.Tenants.SingleAsync(t => t.Id == BootstrapId)).ZitadelOrgId.ShouldBeNull();
    }

    [Fact]
    public async Task Given_bootstrap_org_configured_When_that_org_signs_in_Then_it_claims_the_bootstrap_tenant()
    {
        // Given: an unclaimed bootstrap tenant and BootstrapOrgId = org-jamey
        await using var db = CreateDb();
        SeedUnclaimedBootstrap(db);
        var provisioner = CreateProvisioner(db, bootstrapOrgId: "org-jamey");

        // When: org-jamey signs in
        var id = await provisioner.EnsureTenantAsync("org-jamey", "Jamey Org", CancellationToken.None);

        // Then: the bootstrap row is bound to that org, no second tenant created
        id.ShouldBe(BootstrapId);
        var row = await db.Tenants.SingleAsync();
        row.ZitadelOrgId.ShouldBe("org-jamey");
        row.Name.ShouldBe("Jamey Org");
    }

    [Fact]
    public async Task Given_bootstrap_org_with_padding_When_it_signs_in_Then_match_is_trimmed()
    {
        // Given: config value with stray whitespace (common when pasting into a dashboard)
        await using var db = CreateDb();
        SeedUnclaimedBootstrap(db);
        var provisioner = CreateProvisioner(db, bootstrapOrgId: "  org-jamey ");

        // When: the org signs in
        var id = await provisioner.EnsureTenantAsync("org-jamey", "Jamey Org", CancellationToken.None);

        // Then: it still claims the bootstrap row
        id.ShouldBe(BootstrapId);
    }

    [Fact]
    public async Task Given_no_org_claim_When_provisioning_Then_no_tenant_is_created()
    {
        // Given: an empty database
        await using var db = CreateDb();
        var provisioner = CreateProvisioner(db, bootstrapOrgId: null);

        // When: sign-in arrives without an org id
        var act = () => provisioner.EnsureTenantAsync(null, "No Org", CancellationToken.None);

        // Then: provisioning refuses, and no orphan (claimable) tenant row exists
        await Should.ThrowAsync<ArgumentException>(act);
        (await db.Tenants.CountAsync()).ShouldBe(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Given_blank_org_claim_When_provisioning_Then_no_tenant_is_created(string orgId)
    {
        // Given: an empty database
        await using var db = CreateDb();
        var provisioner = CreateProvisioner(db, bootstrapOrgId: null);

        // When: sign-in arrives with a blank org id
        var act = () => provisioner.EnsureTenantAsync(orgId, "Blank", CancellationToken.None);

        // Then: refused
        await Should.ThrowAsync<ArgumentException>(act);
        (await db.Tenants.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Given_existing_org_When_it_signs_in_again_Then_same_tenant_is_returned()
    {
        // Given: org-abc already has a tenant
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
        var provisioner = CreateProvisioner(db, bootstrapOrgId: null);

        // When: org-abc signs in again
        var id = await provisioner.EnsureTenantAsync("org-abc", "Other Name", CancellationToken.None);

        // Then: no duplicate tenant
        id.ShouldBe(existingId);
        (await db.Tenants.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task Given_new_org_When_provisioned_Then_tenant_is_on_free_plan()
    {
        // Given: an empty database
        await using var db = CreateDb();
        var provisioner = CreateProvisioner(db, bootstrapOrgId: null);

        // When: a new org signs in
        var id = await provisioner.EnsureTenantAsync("org-new", "New Org", CancellationToken.None);

        // Then: free plan, bound to the org
        var row = await db.Tenants.SingleAsync(t => t.Id == id);
        row.Plan.ShouldBe("free");
        row.ZitadelOrgId.ShouldBe("org-new");
    }

    internal static TenantProvisioner CreateProvisioner(QikLogDbContext db, string? bootstrapOrgId) =>
        new(
            db,
            Options.Create(new QikLogAuthOptions { BootstrapOrgId = bootstrapOrgId }),
            NullLogger<TenantProvisioner>.Instance);

    private static void SeedUnclaimedBootstrap(QikLogDbContext db)
    {
        db.Tenants.Add(new TenantEntity
        {
            Id = BootstrapId,
            Name = "QikLog Bootstrap",
            ZitadelOrgId = null,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            Plan = "free"
        });
        db.SaveChanges();
    }

    private static QikLogDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<QikLogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new QikLogDbContext(options);
    }
}
