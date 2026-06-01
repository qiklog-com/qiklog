using Microsoft.AspNetCore.SignalR.Client;
using Shouldly;
using Xunit;

namespace QikLog.Api.Tests;

[Trait("Category", "Load")]
public sealed class SignalRLoadTests(QikLogApiWebApplicationFactory factory) : IClassFixture<QikLogApiWebApplicationFactory>
{
    [Fact]
    public async Task Hub_supports_100_concurrent_subscriptions()
    {
        var baseAddress = factory.Server.BaseAddress
            ?? throw new InvalidOperationException("Test server has no base address.");

        var hubUrl = new Uri(baseAddress, "/hubs/logs");
        var connections = new List<HubConnection>();

        try
        {
            for (var i = 0; i < 100; i++)
            {
                var connection = new HubConnectionBuilder()
                    .WithUrl(hubUrl, options =>
                    {
                        options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                    })
                    .Build();

                await connection.StartAsync();
                await connection.InvokeAsync("Subscribe", $"load-source-{i}");
                connections.Add(connection);
            }

            connections.Count.ShouldBe(100);
            connections.TrueForAll(c => c.State == HubConnectionState.Connected).ShouldBeTrue();
        }
        finally
        {
            foreach (var connection in connections)
            {
                await connection.DisposeAsync();
            }
        }
    }
}
