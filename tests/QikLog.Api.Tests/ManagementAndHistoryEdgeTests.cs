using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QikLog.Api.Auth.Testing;
using QikLog.Core;
using QikLog.Core.Management;
using QikLog.Infrastructure.Auth;
using QikLog.Infrastructure.Data;
using QikLog.Infrastructure.Tenants;
using Shouldly;
using Xunit;

namespace QikLog.Api.Tests;

public sealed class ManagementAndHistoryEdgeTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new LogLevelJsonConverter() }
    };

    private readonly QikLogApiWebApplicationFactory _factory = new();
    private HttpClient _jwtClient = null!;
    private HttpClient _ingestClient = null!;
    private string _apiKey = "";

    public async Task InitializeAsync()
    {
        _apiKey = await ApiTestData.CreateApiKeyForPrimaryTenantAsync(_factory.Services);
        _jwtClient = _factory.CreateClient();
        ApiTestAuth.SetValidJwt(_jwtClient);
        _ingestClient = _factory.CreateClient();
        ApiTestAuth.SetApiKey(_ingestClient, _apiKey);
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Theory]
    [InlineData("""{"name":""}""")]
    [InlineData("""{"name":"   "}""")]
    public async Task Create_key_blank_name_returns_bad_request(string json)
    {
        var response = await _jwtClient.PostAsync(
            "/v1/keys",
            new StringContent(json, Encoding.UTF8, "application/json"));
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Revoke_unknown_key_returns_not_found()
    {
        var response = await _jwtClient.PostAsync($"/v1/keys/{Guid.NewGuid()}/revoke", null);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task History_respects_limit_query()
    {
        var source = $"lim-{Guid.NewGuid():N}"[..12];
        for (var i = 0; i < 5; i++)
        {
            await _ingestClient.PostAsync(
                "/v1/logs",
                new StringContent(
                    $$"""{"source":"{{source}}","message":"m{{i}}"}""",
                    Encoding.UTF8,
                    "application/json"));
        }

        var response = await _ingestClient.GetAsync($"/v1/sources/{source}/logs?limit=2");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var entries = await response.Content.ReadFromJsonAsync<List<LogEntry>>(JsonOptions);
        entries!.Count.ShouldBe(2);
        entries[0].Message.ShouldBe("m3");
        entries[1].Message.ShouldBe("m4");
    }

    [Fact]
    public async Task History_tenant_a_cannot_see_tenant_b_entries()
    {
        var source = $"iso-{Guid.NewGuid():N}"[..12];
        string tenantBKey;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QikLogDbContext>();
            var tenantB = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
            if (!await db.Tenants.AnyAsync(t => t.Id == tenantB))
            {
                db.Tenants.Add(new TenantEntity
                {
                    Id = tenantB,
                    Name = "Tenant B",
                    Plan = "free",
                    CreatedAt = DateTimeOffset.UtcNow
                });
                await db.SaveChangesAsync();
            }

            var tenant = scope.ServiceProvider.GetRequiredService<ITenantContext>();
            tenant.TenantId = tenantB;
            var keys = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
            tenantBKey = (await keys.CreateAsync("b-key", CancellationToken.None)).Plaintext;
        }

        var bClient = _factory.CreateClient();
        ApiTestAuth.SetApiKey(bClient, tenantBKey);
        await bClient.PostAsync(
            "/v1/logs",
            new StringContent(
                $$"""{"source":"{{source}}","message":"secret-b"}""",
                Encoding.UTF8,
                "application/json"));

        var response = await _ingestClient.GetAsync($"/v1/sources/{source}/logs?limit=10");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var entries = await response.Content.ReadFromJsonAsync<List<LogEntry>>(JsonOptions);
        entries.ShouldNotBeNull();
        entries!.ShouldBeEmpty();
    }

    [Fact]
    public async Task List_sources_tenant_a_cannot_see_tenant_b_sources()
    {
        var source = $"srcb-{Guid.NewGuid():N}"[..12];
        string tenantBKey;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QikLogDbContext>();
            var tenantB = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
            if (!await db.Tenants.AnyAsync(t => t.Id == tenantB))
            {
                db.Tenants.Add(new TenantEntity
                {
                    Id = tenantB,
                    Name = "Tenant D",
                    Plan = "free",
                    CreatedAt = DateTimeOffset.UtcNow
                });
                await db.SaveChangesAsync();
            }

            var tenant = scope.ServiceProvider.GetRequiredService<ITenantContext>();
            tenant.TenantId = tenantB;
            var keys = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
            tenantBKey = (await keys.CreateAsync("d-key", CancellationToken.None)).Plaintext;
        }

        var bClient = _factory.CreateClient();
        ApiTestAuth.SetApiKey(bClient, tenantBKey);
        await bClient.PostAsync(
            "/v1/logs",
            new StringContent(
                $$"""{"source":"{{source}}","message":"hidden"}""",
                Encoding.UTF8,
                "application/json"));

        var sources = await _jwtClient.GetFromJsonAsync<List<SourceSummary>>("/v1/sources", JsonOptions);
        sources.ShouldNotBeNull();
        sources!.Any(s => s.Name == source).ShouldBeFalse();
    }

    [Fact]
    public async Task Ingest_with_properties_and_timestamp_roundtrips_via_history()
    {
        var source = $"props-{Guid.NewGuid():N}"[..12];
        var ts = "2026-07-15T18:30:00Z";
        var payload =
            "{\"source\":\"" + source +
            "\",\"message\":\"rich\",\"level\":\"warn\",\"timestamp\":\"" + ts +
            "\",\"properties\":{\"env\":\"prod\",\"region\":\"iad\"}}";
        await _ingestClient.PostAsync(
            "/v1/logs",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        var response = await _ingestClient.GetAsync($"/v1/sources/{source}/logs?limit=5");
        var entries = await response.Content.ReadFromJsonAsync<List<LogEntry>>(JsonOptions);
        var entry = entries!.Single();
        entry.Message.ShouldBe("rich");
        entry.Level.ShouldBe(LogLevel.Warning);
        entry.Timestamp.ShouldBe(DateTimeOffset.Parse(ts));
        entry.Properties!["env"].ShouldBe("prod");
        entry.Properties["region"].ShouldBe("iad");
    }

    [Theory]
    [InlineData("warn", LogLevel.Warning)]
    [InlineData("err", LogLevel.Error)]
    [InlineData("crit", LogLevel.Critical)]
    public async Task Ingest_level_aliases_accepted(string level, LogLevel expected)
    {
        var source = $"lvl-{Guid.NewGuid():N}"[..12];
        var response = await _ingestClient.PostAsync(
            "/v1/logs",
            new StringContent(
                $$"""{"source":"{{source}}","message":"alias","level":"{{level}}"}""",
                Encoding.UTF8,
                "application/json"));
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        var history = await _ingestClient.GetFromJsonAsync<List<LogEntry>>(
            $"/v1/sources/{source}/logs?limit=1",
            JsonOptions);
        history!.Single().Level.ShouldBe(expected);
    }
}
