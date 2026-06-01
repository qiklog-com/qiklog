using Microsoft.AspNetCore.SignalR.Client;
using Shouldly;
using Xunit;

namespace QikLog.Api.Tests;

[Trait("Category", "Load")]
public sealed class SignalRLoadTests : IAsyncLifetime
{
    private readonly QikLogApiWebApplicationFactory _factory = new();
    private string _apiKey = "";

    public async Task InitializeAsync() =>
        _apiKey = await ApiTestData.CreateApiKeyForPrimaryTenantAsync(_factory.Services);

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Hub_supports_100_concurrent_subscriptions()
    {
        var baseAddress = _factory.Server.BaseAddress
            ?? throw new InvalidOperationException("Test server has no base address.");

        var hubUrl = new Uri(baseAddress, "/hubs/logs");

        await using var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Headers.Add("X-QikLog-API-Key", _apiKey);
            })
            .Build();

        await connection.StartAsync();
        connection.State.ShouldBe(HubConnectionState.Connected);

        for (var i = 0; i < 100; i++)
            await connection.InvokeAsync("Subscribe", $"load-source-{i}");

        connection.State.ShouldBe(HubConnectionState.Connected);
    }
}
