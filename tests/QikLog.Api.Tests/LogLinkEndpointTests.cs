using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QikLog.Core;
using QikLog.Core.Links;
using QikLog.Infrastructure.Data;
using Shouldly;
using Xunit;

namespace QikLog.Api.Tests;

/// <summary>
/// Scenarios from docs/specs/log-link.md. The host runs with auth enforcement on,
/// so every pass here is a link working with no credential.
/// </summary>
public sealed class LogLinkEndpointTests : IAsyncLifetime
{
    private const string FormType = "application/x-www-form-urlencoded";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new LogLevelJsonConverter() }
    };

    private readonly QikLogApiWebApplicationFactory _factory = new();
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Given_auth_enforcement_on_When_a_visitor_creates_a_link_Then_201_with_id_url_and_expiry()
    {
        // Given: no credential on the client
        // When
        var response = await _client.PostAsync("/l", null);

        // Then
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var link = await response.Content.ReadFromJsonAsync<CreatedLink>(JsonOptions);
        link.ShouldNotBeNull();
        LogLinkId.IsValid(link.Id).ShouldBeTrue();
        link.Url.ShouldBe($"http://localhost/l/{link.Id}");
        link.Raw.ShouldBe($"http://localhost/l/{link.Id}/raw");
        link.MaxLines.ShouldBe(500);
        link.Send.ShouldBe($"curl http://localhost/l/{link.Id} -d \"it broke\"");
        link.ExpiresAt.ShouldBeInRange(DateTimeOffset.UtcNow.AddHours(23), DateTimeOffset.UtcNow.AddHours(25));
    }

    [Fact]
    public async Task Given_a_link_When_curl_style_line_is_sent_Then_202_and_raw_shows_it()
    {
        // Given
        var link = await CreateLinkAsync();

        // When: exactly what `curl <url> -d "it broke"` sends
        var send = await SendAsync(link.Id, "it broke");
        var raw = await _client.GetAsync($"/l/{link.Id}/raw");

        // Then
        send.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        var accepted = await send.Content.ReadFromJsonAsync<Accepted>(JsonOptions);
        accepted!.AcceptedCount.ShouldBe(1);
        accepted.Remaining.ShouldBe(499);

        raw.StatusCode.ShouldBe(HttpStatusCode.OK);
        raw.Content.Headers.ContentType!.MediaType.ShouldBe("text/plain");
        var text = await raw.Content.ReadAsStringAsync();
        text.ShouldEndWith("Z INFO  it broke\n");
    }

    [Fact]
    public async Task Given_a_link_When_lines_with_levels_are_sent_Then_levels_are_stored()
    {
        // Given
        var link = await CreateLinkAsync();

        // When: a leading token, a JSON body, and a ?level= override
        await SendAsync(link.Id, "ERROR 500 POST /checkout");
        await SendAsync(link.Id, """{"message":"slow query","level":"warn"}""", "application/json");
        await _client.PostAsync($"/l/{link.Id}?level=debug", new StringContent("cache warm", Encoding.UTF8, "text/plain"));

        // Then
        var lines = await _client.GetFromJsonAsync<List<LogEntry>>($"/l/{link.Id}/lines", JsonOptions);
        lines!.Select(l => (l.Level, l.Message)).ShouldBe(
        [
            (Core.LogLevel.Error, "ERROR 500 POST /checkout"),
            (Core.LogLevel.Warning, "slow query"),
            (Core.LogLevel.Debug, "cache warm")
        ]);
    }

    [Fact]
    public async Task Given_a_link_When_a_multi_line_body_is_piped_Then_each_line_is_stored_and_last_limits_the_read()
    {
        // Given
        var link = await CreateLinkAsync();

        // When
        var send = await SendAsync(link.Id, "boot\nlistening on 8080\nERROR boom\n", "text/plain");
        var tail = await (await _client.GetAsync($"/l/{link.Id}/raw?last=2")).Content.ReadAsStringAsync();

        // Then
        (await send.Content.ReadFromJsonAsync<Accepted>(JsonOptions))!.AcceptedCount.ShouldBe(3);
        var shown = tail.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        shown.Length.ShouldBe(2);
        shown[0].ShouldEndWith("INFO  listening on 8080");
        shown[1].ShouldEndWith("ERROR ERROR boom");
    }

    [Fact]
    public async Task Given_a_plain_client_When_it_gets_the_link_Then_it_reads_text_and_a_browser_is_redirected_to_the_tail()
    {
        // Given: a web app origin is configured
        await using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["QikLog:Links:WebBaseUrl"] = "https://app.example.test/",
                ["QikLog:Links:PublicBaseUrl"] = "https://example.test"
            })));
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var link = (await (await client.PostAsync("/l", null)).Content.ReadFromJsonAsync<CreatedLink>(JsonOptions))!;
        await client.PostAsync($"/l/{link.Id}", new StringContent("hello", Encoding.UTF8, FormType));

        // When: curl-like and browser-like requests open the same address
        var plain = await client.GetAsync($"/l/{link.Id}");
        var browserRequest = new HttpRequestMessage(HttpMethod.Get, $"/l/{link.Id}");
        browserRequest.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,*/*;q=0.8");
        var browser = await client.SendAsync(browserRequest);

        // Then
        link.Url.ShouldBe($"https://example.test/l/{link.Id}");
        plain.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await plain.Content.ReadAsStringAsync()).ShouldContain("INFO  hello");
        browser.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        browser.Headers.Location!.ToString().ShouldBe($"https://app.example.test/l/{link.Id}");
    }

    [Theory]
    [InlineData("zzzzzzzzzzzz")]
    [InlineData("short")]
    [InlineData("K7MQ2X9D4FAB")]
    public async Task Given_an_unknown_or_malformed_id_When_used_Then_404(string id)
    {
        (await SendAsync(id, "x")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await _client.GetAsync($"/l/{id}/raw")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await _client.GetAsync($"/l/{id}/lines")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await _client.GetAsync($"/l/{id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Given_an_expired_link_When_a_line_is_sent_Then_410_and_nothing_is_stored()
    {
        // Given
        var link = await CreateLinkAsync();
        await UpdateLinkAsync(link.Id, l => l.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1));

        // When
        var send = await SendAsync(link.Id, "late");
        var read = await _client.GetAsync($"/l/{link.Id}/raw");

        // Then
        send.StatusCode.ShouldBe(HttpStatusCode.Gone);
        read.StatusCode.ShouldBe(HttpStatusCode.Gone);
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QikLogDbContext>();
        (await db.LogEntries.CountAsync(e => e.Source == LogLinkId.SourceFor(link.Id))).ShouldBe(0);
    }

    [Fact]
    public async Task Given_a_full_link_When_a_line_is_sent_Then_402_and_reading_still_works()
    {
        // Given
        var link = await CreateLinkAsync();
        await SendAsync(link.Id, "kept");
        await UpdateLinkAsync(link.Id, l => l.LineCount = 500);

        // When
        var send = await SendAsync(link.Id, "one too many");
        var read = await _client.GetAsync($"/l/{link.Id}/raw");

        // Then: the cap blocks sending only
        send.StatusCode.ShouldBe(HttpStatusCode.PaymentRequired);
        (await send.Content.ReadAsStringAsync()).ShouldContain("Reading still works");
        read.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await read.Content.ReadAsStringAsync()).ShouldContain("kept");
    }

    [Fact]
    public async Task Given_a_link_When_the_body_is_empty_or_oversized_Then_400_or_413()
    {
        // Given
        var link = await CreateLinkAsync();

        // When / Then
        (await SendAsync(link.Id, "")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await SendAsync(link.Id, "   \n ")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await SendAsync(link.Id, new string('x', 2049))).StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
        (await SendAsync(link.Id, string.Join('\n', Enumerable.Repeat("line", 51)), "text/plain"))
            .StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
        (await SendAsync(link.Id, new string('x', 200_000))).StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
        (await SendAsync(link.Id, new string('x', 2048))).StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Given_a_link_line_and_a_tenant_key_When_the_tenant_reads_history_and_sources_Then_the_link_is_invisible()
    {
        // Given
        var link = await CreateLinkAsync();
        await SendAsync(link.Id, "not yours");
        var apiKey = await ApiTestData.CreateApiKeyForPrimaryTenantAsync(_factory.Services);
        var tenant = _factory.CreateClient();
        ApiTestAuth.SetApiKey(tenant, apiKey);
        var signedIn = _factory.CreateClient();
        ApiTestAuth.SetValidJwt(signedIn);

        // When
        var history = await tenant.GetFromJsonAsync<List<LogEntry>>(
            $"/v1/sources/{LogLinkId.SourceFor(link.Id)}/logs",
            JsonOptions);
        var sources = await signedIn.GetStringAsync("/v1/sources");
        var status = await signedIn.GetStringAsync("/v1/billing/status");

        // Then: no history, no source, and no usage counted against the tenant
        history.ShouldBeEmpty();
        sources.ShouldNotContain(link.Id);
        status.ShouldContain("\"usedThisMonth\":0");
    }

    [Fact]
    public async Task Given_a_per_address_limit_When_it_is_passed_Then_429()
    {
        // Given
        await using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["QikLog:Links:CreatesPerIpPerHour"] = "2"
            })));
        var client = factory.CreateClient();

        // When / Then
        (await client.PostAsync("/l", null)).StatusCode.ShouldBe(HttpStatusCode.Created);
        (await client.PostAsync("/l", null)).StatusCode.ShouldBe(HttpStatusCode.Created);
        (await client.PostAsync("/l", null)).StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Given_a_page_on_another_origin_When_it_creates_and_sends_Then_cors_allows_it_without_credentials()
    {
        // Given: a browser on the marketing site
        var link = await CreateLinkAsync();
        var request = new HttpRequestMessage(HttpMethod.Post, $"/l/{link.Id}")
        {
            Content = new StringContent("from a page", Encoding.UTF8, "text/plain")
        };
        request.Headers.Add("Origin", "https://qiklog.com");

        // When
        var response = await _client.SendAsync(request);

        // Then
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        response.Headers.GetValues("Access-Control-Allow-Origin").ShouldBe(["*"]);
        response.Headers.Contains("Access-Control-Allow-Credentials").ShouldBeFalse();
    }

    [Fact]
    public async Task Given_a_hub_subscriber_on_a_link_When_a_line_is_sent_Then_only_that_link_is_received()
    {
        // Given: the web app's server-side connection (it signs in with an API key)
        var mine = await CreateLinkAsync();
        var other = await CreateLinkAsync();
        var apiKey = await ApiTestData.CreateApiKeyForPrimaryTenantAsync(_factory.Services, "web-server");
        var received = new List<LogEntry>();
        var gotMine = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/logs", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
                options.Headers["X-QikLog-API-Key"] = apiKey;
            })
            .Build();
        connection.On<LogEntry>("LogReceived", entry =>
        {
            lock (received)
                received.Add(entry);
            if (entry.Message == "for me")
                gotMine.TrySetResult();
        });
        await connection.StartAsync();
        await connection.InvokeAsync("SubscribeLink", mine.Id);
        await connection.InvokeAsync("SubscribeLink", "not-a-link-id");

        // When
        await SendAsync(other.Id, "for someone else");
        await SendAsync(mine.Id, "for me");
        await gotMine.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // Then
        lock (received)
        {
            received.ShouldHaveSingleItem().Message.ShouldBe("for me");
            received[0].Source.ShouldBe(LogLinkId.SourceFor(mine.Id));
        }

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task Given_expired_links_When_a_new_link_is_created_Then_the_expired_ones_and_their_lines_are_deleted()
    {
        // Given
        var old = await CreateLinkAsync();
        await SendAsync(old.Id, "stale");
        await UpdateLinkAsync(old.Id, l => l.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5));

        // When
        var fresh = await CreateLinkAsync();

        // Then
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QikLogDbContext>();
        (await db.LogLinks.Select(l => l.Id).ToListAsync()).ShouldBe([fresh.Id]);
        (await db.LogEntries.CountAsync()).ShouldBe(0);
        (await _client.GetAsync($"/l/{old.Id}/raw")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<CreatedLink> CreateLinkAsync()
    {
        var response = await _client.PostAsync("/l", null);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<CreatedLink>(JsonOptions))!;
    }

    private Task<HttpResponseMessage> SendAsync(string id, string body, string contentType = FormType)
    {
        var content = new StringContent(body, Encoding.UTF8);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        return _client.PostAsync($"/l/{id}", content);
    }

    private async Task UpdateLinkAsync(string id, Action<LogLinkEntity> change)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QikLogDbContext>();
        var link = await db.LogLinks.SingleAsync(l => l.Id == id);
        change(link);
        await db.SaveChangesAsync();
    }

    private sealed record CreatedLink(string Id, string Url, string Raw, DateTimeOffset ExpiresAt, int MaxLines, string Send);

    private sealed record Accepted(
        [property: System.Text.Json.Serialization.JsonPropertyName("accepted")] int AcceptedCount,
        int Remaining);
}
