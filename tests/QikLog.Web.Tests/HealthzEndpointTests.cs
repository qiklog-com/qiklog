using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QikLog.Infrastructure.Data;
using Shouldly;
using Xunit;

namespace QikLog.Web.Tests;

/// <summary>
/// Web's /healthz is the Container Apps readiness probe. Web can start before the API or the
/// migrate job has created data_protection_keys, so readiness must depend on that table.
/// </summary>
public sealed class HealthzEndpointTests
{
    // Port 1 refuses connections immediately; nothing is listening.
    private const string UnreachablePostgres = "Host=127.0.0.1;Port=1;Database=qiklog;Username=qiklog;Timeout=2";
    private const string AnyPostgres = "Host=postgres.invalid;Database=qiklog;Username=qiklog";

    [Fact]
    public async Task Given_keys_table_is_queryable_When_probing_healthz_Then_returns_200()
    {
        // Given: Web configured with Postgres, and the keys table exists
        await using var factory = new WebFactory(AnyPostgres, useInMemoryDatabase: true);
        using var client = factory.CreateClient();

        // When: the readiness probe calls /healthz
        var response = await client.GetAsync("/healthz");

        // Then: ready
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthzBody>();
        body.ShouldNotBeNull();
        body.Status.ShouldBe("ok");
        body.DataProtectionKeys.ShouldBe("ok");
    }

    [Fact]
    public async Task Given_database_unreachable_When_probing_healthz_Then_returns_503()
    {
        // Given: Web configured with Postgres that cannot be reached (or has no keys table yet)
        await using var factory = new WebFactory(UnreachablePostgres, useInMemoryDatabase: false);
        using var client = factory.CreateClient();

        // When: the readiness probe calls /healthz
        var response = await client.GetAsync("/healthz");

        // Then: not ready, so Container Apps keeps traffic away from this revision
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        var body = await response.Content.ReadFromJsonAsync<HealthzBody>();
        body.ShouldNotBeNull();
        body.Status.ShouldBe("degraded");
        body.DataProtectionKeys.ShouldBe("unavailable");
    }

    [Fact]
    public async Task Given_oidc_enabled_When_anonymous_probe_calls_healthz_Then_not_challenged()
    {
        // Given: sign-in is required for the dashboard
        await using var factory = new WebFactory(AnyPostgres, useInMemoryDatabase: true, oidcEnabled: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // When: an anonymous probe calls /healthz
        var response = await client.GetAsync("/healthz");

        // Then: answered directly, no redirect to Zitadel and no 401
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Given_no_postgres_When_probing_healthz_Then_returns_200_with_keys_on_disk()
    {
        // Given: local dev without a database
        await using var factory = new WebFactory(connectionString: null, useInMemoryDatabase: false);
        using var client = factory.CreateClient();

        // When: /healthz is called
        var response = await client.GetAsync("/healthz");

        // Then: ready; the key store check is skipped
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthzBody>();
        body.ShouldNotBeNull();
        body.DataProtectionKeys.ShouldBe("skipped");
    }

    private sealed record HealthzBody(string Status, string DataProtectionKeys);

    private sealed class WebFactory(string? connectionString, bool useInMemoryDatabase, bool oidcEnabled = false)
        : WebApplicationFactory<Program>
    {
        private readonly string _keysPath = Path.Combine(Path.GetTempPath(), $"qiklog-web-tests-{Guid.NewGuid():N}");
        private readonly string _databaseName = $"QikLogWebTests_{Guid.NewGuid():N}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            // UseSetting is visible to Program.cs before Build (it reads the connection string early).
            builder.UseSetting("ConnectionStrings:Postgres", connectionString ?? string.Empty);
            builder.UseSetting("DataProtection:KeysPath", _keysPath);
            builder.UseSetting("QikLog:Auth:Enabled", oidcEnabled ? "true" : "false");
            builder.UseSetting("QikLog:Auth:Authority", oidcEnabled ? "https://zitadel.invalid" : string.Empty);

            if (!useInMemoryDatabase)
                return;

            builder.ConfigureTestServices(services =>
            {
                // Swap Npgsql for an in-memory database that already has the keys table.
                services.RemoveAll<DbContextOptions<QikLogDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<QikLogDbContext>>();
                services.AddDbContext<QikLogDbContext>(o => o.UseInMemoryDatabase(_databaseName));
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && Directory.Exists(_keysPath))
                Directory.Delete(_keysPath, recursive: true);
        }
    }
}
