using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Xunit;

namespace QikLog.Api.Tests;

public sealed class UsageLimitApiWebApplicationFactory : QikLogApiWebApplicationFactory
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["QikLog:Usage:FreeIngestPerMonth"] = "2"
            });
        });
    }
}

public sealed class UsageLimitTests(UsageLimitApiWebApplicationFactory factory) : IClassFixture<UsageLimitApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Ingest_over_free_limit_returns_payment_required()
    {
        for (var i = 0; i < 2; i++)
        {
            var ok = await _client.PostAsync(
                "/v1/logs",
                new StringContent("""{"source":"cap","message":"ok"}""", Encoding.UTF8, "application/json"));
            ok.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        }

        var blocked = await _client.PostAsync(
            "/v1/logs",
            new StringContent("""{"source":"cap","message":"nope"}""", Encoding.UTF8, "application/json"));
        blocked.StatusCode.ShouldBe(HttpStatusCode.PaymentRequired);
    }
}
