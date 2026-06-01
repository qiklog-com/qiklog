using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR.Client;
using QikLog.Api.Auth.Testing;
using QikLog.Core;
using QikLog.Infrastructure.Data;
using QikLog.Infrastructure.Tenants;
using Shouldly;
using Xunit;

namespace QikLog.Api.Tests;

public sealed class SignalRHubAuthTests : IAsyncLifetime
{
    private readonly QikLogApiWebApplicationFactory _factory = new();
    private string _apiKeyA = "";
    private string _apiKeyB = "";
    private Guid _tenantBId;

    public async Task InitializeAsync()
    {
        await ApiTestData.SeedPrimaryTenantAsync(_factory.Services);
        _apiKeyA = await ApiTestData.CreateApiKeyForPrimaryTenantAsync(_factory.Services, "hub-a");

        _tenantBId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QikLogDbContext>();
            db.Tenants.Add(new TenantEntity
            {
                Id = _tenantBId,
                Name = "Tenant B",
                Plan = "free",
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();

            var tenant = scope.ServiceProvider.GetRequiredService<ITenantContext>();
            tenant.TenantId = _tenantBId;
            var keys = scope.ServiceProvider.GetRequiredService<QikLog.Infrastructure.Auth.IApiKeyService>();
            var created = await keys.CreateAsync("hub-b", CancellationToken.None);
            _apiKeyB = created.Plaintext;
        }
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Hub_without_auth_fails_to_connect()
    {
        var connection = CreateConnection();
        await Should.ThrowAsync<HttpRequestException>(() => connection.StartAsync());
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task Hub_with_valid_jwt_connects()
    {
        var connection = CreateConnection(TestAuthHandler.ValidToken);
        await connection.StartAsync();
        connection.State.ShouldBe(HubConnectionState.Connected);
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task Hub_with_unknown_tenant_jwt_fails_to_connect()
    {
        var connection = CreateConnection(TestAuthHandler.UnknownTenantToken);
        await Should.ThrowAsync<HttpRequestException>(() => connection.StartAsync());
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task Hub_with_malformed_jwt_fails_to_connect()
    {
        var connection = CreateConnection(TestAuthHandler.MalformedToken);
        await Should.ThrowAsync<HttpRequestException>(() => connection.StartAsync());
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task Hub_with_valid_api_key_connects()
    {
        var connection = CreateConnection(apiKey: _apiKeyA);
        await connection.StartAsync();
        connection.State.ShouldBe(HubConnectionState.Connected);
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task Hub_with_invalid_api_key_fails_to_connect()
    {
        var connection = CreateConnection(apiKey: "ql_00000000_invalidsecretpart0000");
        await Should.ThrowAsync<HttpRequestException>(() => connection.StartAsync());
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task Subscriber_tenant_a_does_not_receive_tenant_b_logs()
    {
        var received = new TaskCompletionSource<LogEntry>(TaskCreationOptions.RunContinuationsAsynchronously);
        var connection = CreateConnection(apiKey: _apiKeyA);
        connection.On<LogEntry>("LogReceived", entry => received.TrySetResult(entry));

        await connection.StartAsync();
        await connection.InvokeAsync("Subscribe", "shared-source");

        var ingestB = _factory.CreateClient();
        ApiTestAuth.SetApiKey(ingestB, _apiKeyB);
        var response = await ingestB.PostAsync(
            "/v1/logs",
            new StringContent(
                """{"source":"shared-source","message":"from-tenant-b"}""",
                Encoding.UTF8,
                "application/json"));
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(2)));
        completed.ShouldNotBe(received.Task);

        var ingestA = _factory.CreateClient();
        ApiTestAuth.SetApiKey(ingestA, _apiKeyA);
        await ingestA.PostAsync(
            "/v1/logs",
            new StringContent(
                """{"source":"shared-source","message":"from-tenant-a"}""",
                Encoding.UTF8,
                "application/json"));

        var entry = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        entry.Message.ShouldBe("from-tenant-a");

        await connection.DisposeAsync();
    }

    private HubConnection CreateConnection(string? jwt = null, string? apiKey = null)
    {
        var baseAddress = _factory.Server.BaseAddress
            ?? throw new InvalidOperationException("Test server has no base address.");

        return new HubConnectionBuilder()
            .WithUrl(new Uri(baseAddress, "/hubs/logs"), options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                if (!string.IsNullOrWhiteSpace(jwt))
                    options.Headers.Add("Authorization", $"Bearer {jwt}");
                if (!string.IsNullOrWhiteSpace(apiKey))
                    options.Headers.Add("X-QikLog-API-Key", apiKey);
            })
            .Build();
    }
}

public sealed class DevKeysEndpointTests
{
    [Fact]
    public async Task Dev_keys_returns_not_found_in_production()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.UseSetting("ConnectionStrings:Postgres", string.Empty);
                builder.ConfigureServices(services =>
                {
                    services.AddDbContext<QikLogDbContext>(options =>
                        options.UseInMemoryDatabase($"DevKeysProd_{Guid.NewGuid():N}"));
                    services.AddScoped<ITenantContext, TenantContext>();
                    services.AddScoped<TenantProvisioner>();
                    services.AddScoped<TenantResolver>();
                });
            });

        var client = factory.CreateClient();
        var response = await client.PostAsync(
            "/v1/dev/keys",
            new StringContent("""{"name":"nope"}""", Encoding.UTF8, "application/json"));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
