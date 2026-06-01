using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QikLog.Infrastructure.Data;
using Shouldly;
using Xunit;

namespace QikLog.Api.Tests;

public sealed class IngestEndpointTests : IClassFixture<QikLogApiWebApplicationFactory>
{
    private readonly QikLogApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public IngestEndpointTests(QikLogApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Healthz_returns_ok()
    {
        var response = await _client.GetAsync("/healthz");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        body!.Status.ShouldBe("ok");
        body.Postgres.ShouldBe("ok");
    }

    [Fact]
    public async Task Ingest_persists_entry_to_database()
    {
        var response = await PostLogsAsync("""{"source":"persist","message":"stored"}""");
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QikLogDbContext>();
        var count = await db.LogEntries.CountAsync(e => e.Source == "persist");
        count.ShouldBe(1);
    }

    [Theory]
    [InlineData("""{"source":"demo","level":"info","message":"hello"}""")]
    [InlineData("""{"source":"demo","level":2,"message":"hello"}""")]
    [InlineData("""{"source":"demo","message":"hello"}""")]
    public async Task Ingest_valid_payload_returns_accepted(string json)
    {
        var response = await PostLogsAsync(json);
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Ingest_trims_source_name()
    {
        var response = await PostLogsAsync("""{"source":"  demo  ","message":"x"}""");
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Theory]
    [InlineData("""{"source":"","message":"hello"}""", "source")]
    [InlineData("""{"source":"   ","message":"hello"}""", "source")]
    [InlineData("""{"source":"demo","message":""}""", "message")]
    [InlineData("""{"source":"demo"}""", "message")]
    public async Task Ingest_missing_required_field_returns_bad_request(string json, string expectedErrorFragment)
    {
        var response = await PostLogsAsync(json);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain(expectedErrorFragment);
    }

    [Fact]
    public async Task Ingest_unknown_level_returns_bad_request()
    {
        var response = await PostLogsAsync("""{"source":"demo","level":"nope","message":"hello"}""");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Ingest_malformed_json_returns_bad_request()
    {
        var content = new StringContent("{not json", Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/v1/logs", content);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private Task<HttpResponseMessage> PostLogsAsync(string json) =>
        _client.PostAsync("/v1/logs", new StringContent(json, Encoding.UTF8, "application/json"));

    private sealed record HealthResponse(string Status, string Postgres);
}
