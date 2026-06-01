using System.Net;
using System.Text;
using Shouldly;
using Xunit;

namespace QikLog.Api.Tests;

public sealed class ObservabilityEndpointTests(QikLogApiWebApplicationFactory factory) : IClassFixture<QikLogApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_returns_version_and_dependency_status()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.ShouldContain("\"status\"");
        json.ShouldContain("\"version\"");
        json.ShouldContain("\"postgres\"");
        json.ShouldContain("\"redis\"");
    }

    [Fact]
    public async Task Metrics_exposes_prometheus_format()
    {
        await _client.PostAsync(
            "/v1/logs",
            new StringContent("""{"source":"metrics","message":"ping"}""", Encoding.UTF8, "application/json"));

        var response = await _client.GetAsync("/metrics");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("qiklog_http_requests_total");
        body.ShouldContain("qiklog_logs_ingested_total");
    }
}
