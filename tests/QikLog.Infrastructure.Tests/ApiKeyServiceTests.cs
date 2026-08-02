using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QikLog.Infrastructure.Auth;
using QikLog.Infrastructure.Data;
using QikLog.Infrastructure.Tenants;
using Shouldly;
using Xunit;

namespace QikLog.Infrastructure.Tests;

public sealed class ApiKeyServiceTests
{
    [Fact]
    public async Task CreateAsync_persists_hash_prefix_and_tenant()
    {
        await using var db = CreateDb();
        var tenantId = await SeedTenantAsync(db);
        var service = CreateService(db, tenantId);

        var created = await service.CreateAsync("  ship key  ", CancellationToken.None);

        created.Plaintext.ShouldStartWith("ql_");
        created.Name.ShouldBe("ship key");
        var row = await db.ApiKeys.SingleAsync();
        row.TenantId.ShouldBe(tenantId);
        row.LookupPrefix.Length.ShouldBe(8);
        row.SecretHash.ShouldContain(".");
        row.IsActive.ShouldBeTrue();
        row.RevokedAt.ShouldBeNull();
    }

    [Fact]
    public async Task ValidateAsync_returns_result_and_updates_LastUsedAt()
    {
        await using var db = CreateDb();
        var tenantId = await SeedTenantAsync(db);
        var service = CreateService(db, tenantId);
        var created = await service.CreateAsync("dev", CancellationToken.None);

        var validation = await service.ValidateAsync(created.Plaintext, CancellationToken.None);

        validation.ShouldNotBeNull();
        validation!.Id.ShouldBe(created.Id);
        validation.TenantId.ShouldBe(tenantId);
        (await db.ApiKeys.SingleAsync()).LastUsedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task ValidateAsync_returns_null_for_unknown_key()
    {
        await using var db = CreateDb();
        var service = CreateService(db, await SeedTenantAsync(db));
        var result = await service.ValidateAsync("ql_zzzzzzzz_secretsecretsecretsecretse", CancellationToken.None);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ValidateAsync_returns_null_for_revoked_key()
    {
        await using var db = CreateDb();
        var service = CreateService(db, await SeedTenantAsync(db));
        var created = await service.CreateAsync("temp", CancellationToken.None);
        await service.RevokeAsync(created.Id, CancellationToken.None);

        (await service.ValidateAsync(created.Plaintext, CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task ValidateAsync_returns_null_for_inactive_key()
    {
        await using var db = CreateDb();
        var tenantId = await SeedTenantAsync(db);
        var service = CreateService(db, tenantId);
        var created = await service.CreateAsync("inactive", CancellationToken.None);
        var row = await db.ApiKeys.SingleAsync();
        row.IsActive = false;
        await db.SaveChangesAsync();

        (await service.ValidateAsync(created.Plaintext, CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task ValidateAsync_returns_null_for_malformed_plaintext()
    {
        await using var db = CreateDb();
        var service = CreateService(db, await SeedTenantAsync(db));
        (await service.ValidateAsync("not-a-key", CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task AnyActiveKeysAsync_true_then_false_after_revoke()
    {
        await using var db = CreateDb();
        var service = CreateService(db, await SeedTenantAsync(db));
        (await service.AnyActiveKeysAsync(CancellationToken.None)).ShouldBeFalse();

        var created = await service.CreateAsync("only", CancellationToken.None);
        (await service.AnyActiveKeysAsync(CancellationToken.None)).ShouldBeTrue();

        await service.RevokeAsync(created.Id, CancellationToken.None);
        (await service.AnyActiveKeysAsync(CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task RevokeAsync_unknown_id_returns_false()
    {
        await using var db = CreateDb();
        var service = CreateService(db, await SeedTenantAsync(db));
        (await service.RevokeAsync(Guid.NewGuid(), CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task RevokeAsync_idempotent_when_already_revoked()
    {
        await using var db = CreateDb();
        var service = CreateService(db, await SeedTenantAsync(db));
        var created = await service.CreateAsync("once", CancellationToken.None);

        (await service.RevokeAsync(created.Id, CancellationToken.None)).ShouldBeTrue();
        (await service.RevokeAsync(created.Id, CancellationToken.None)).ShouldBeTrue();
    }

    private static async Task<Guid> SeedTenantAsync(QikLogDbContext db)
    {
        var id = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity
        {
            Id = id,
            Name = "t",
            Plan = "free",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        return id;
    }

    private static ApiKeyService CreateService(QikLogDbContext db, Guid tenantId) =>
        new(
            db,
            new ApiKeyHasher(),
            Options.Create(new IngestAuthOptions { RateLimitPerMinute = 60 }),
            new TenantContext { TenantId = tenantId },
            NullLogger<ApiKeyService>.Instance);

    private static QikLogDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<QikLogDbContext>()
            .UseInMemoryDatabase($"api-key-service-{Guid.NewGuid():N}")
            .Options;
        var db = new QikLogDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
