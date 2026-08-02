using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QikLog.Infrastructure.Auth;
using QikLog.Infrastructure.Data;
using QikLog.Infrastructure.Tenants;
using Shouldly;
using Xunit;

namespace QikLog.Infrastructure.Tests;

public sealed class TenantResolverTests
{
    [Fact]
    public async Task Resolve_unauthenticated_principal_returns_Unauthenticated()
    {
        await using var db = CreateDb();
        var resolver = CreateResolver(db);
        var result = await resolver.ResolveFromPrincipalAsync(new ClaimsPrincipal(), CancellationToken.None);
        result.Status.ShouldBe(TenantResolutionStatus.Unauthenticated);
    }

    [Fact]
    public async Task Resolve_null_principal_returns_Unauthenticated()
    {
        await using var db = CreateDb();
        var resolver = CreateResolver(db);
        var result = await resolver.ResolveFromPrincipalAsync(null, CancellationToken.None);
        result.Status.ShouldBe(TenantResolutionStatus.Unauthenticated);
    }

    [Fact]
    public async Task Resolve_valid_tenant_id_claim_returns_Success()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = "known",
            Plan = "free",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var principal = Authenticated(("tenant_id", tenantId.ToString()));
        var result = await CreateResolver(db).ResolveFromPrincipalAsync(principal, CancellationToken.None);

        result.Status.ShouldBe(TenantResolutionStatus.Success);
        result.TenantId.ShouldBe(tenantId);
    }

    [Fact]
    public async Task Resolve_unknown_tenant_id_claim_returns_TenantNotFound()
    {
        await using var db = CreateDb();
        var principal = Authenticated(("tenant_id", Guid.NewGuid().ToString()));
        var result = await CreateResolver(db).ResolveFromPrincipalAsync(principal, CancellationToken.None);
        result.Status.ShouldBe(TenantResolutionStatus.TenantNotFound);
    }

    [Fact]
    public async Task Resolve_org_claim_provisions_and_returns_Success()
    {
        await using var db = CreateDb();
        var principal = Authenticated(
            ("urn:zitadel:iam:org:id", "org-42"),
            ("name", "Acme"));
        var result = await CreateResolver(db).ResolveFromPrincipalAsync(principal, CancellationToken.None);

        result.Status.ShouldBe(TenantResolutionStatus.Success);
        result.TenantId.ShouldNotBeNull();
        var row = await db.Tenants.SingleAsync();
        row.ZitadelOrgId.ShouldBe("org-42");
        row.Name.ShouldBe("Acme");
    }

    [Fact]
    public async Task Resolve_missing_org_and_tenant_claim_returns_TenantNotFound()
    {
        await using var db = CreateDb();
        var principal = Authenticated(("name", "lonely"));
        var result = await CreateResolver(db).ResolveFromPrincipalAsync(principal, CancellationToken.None);
        result.Status.ShouldBe(TenantResolutionStatus.TenantNotFound);
    }

    [Theory]
    [InlineData("name", "From Name", "From Name")]
    [InlineData(ClaimTypes.Email, "ops@example.com", "ops@example.com")]
    public async Task Resolve_uses_name_or_email_claim_for_display_name(
        string claimType,
        string claimValue,
        string expectedName)
    {
        await using var db = CreateDb();
        var principal = Authenticated(
            ("urn:zitadel:iam:org:id", $"org-{Guid.NewGuid():N}"[..20]),
            (claimType, claimValue));
        var result = await CreateResolver(db).ResolveFromPrincipalAsync(principal, CancellationToken.None);
        result.Status.ShouldBe(TenantResolutionStatus.Success);
        (await db.Tenants.SingleAsync(t => t.Id == result.TenantId)).Name.ShouldBe(expectedName);
    }

    private static ClaimsPrincipal Authenticated(params (string Type, string Value)[] claims)
    {
        var identity = new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value)), "test");
        return new ClaimsPrincipal(identity);
    }

    private static TenantResolver CreateResolver(QikLogDbContext db) =>
        new(
            db,
            new TenantProvisioner(db, NullLogger<TenantProvisioner>.Instance),
            Options.Create(new QikLogAuthOptions()));

    private static QikLogDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<QikLogDbContext>()
            .UseInMemoryDatabase($"tenant-resolver-{Guid.NewGuid():N}")
            .Options;
        var db = new QikLogDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
