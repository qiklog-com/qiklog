using System.Net;
using System.Text;
using Bunit;
using QikLog.Web.Components.Pages;
using QikLog.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using Xunit;

namespace QikLog.Web.Tests;

public sealed class LinkPageTests : TestContext
{
    private const string LinkId = "k7mq2x9d4fab";
    private const string DemoKey = "ql_demo_secret_value";
    private const string HubKey = "ql_hub_secret_value";

    private sealed class StubApi(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public List<string> Paths { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Paths.Add(request.RequestUri!.PathAndQuery);
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }

    private StubApi Install(HttpStatusCode status, string body, string? linkBaseUrl = "https://qiklog.com")
    {
        var harness = new PlaygroundHarness(DemoKey, HubKey);
        harness.Options.ApiBaseUrl = "";
        harness.Options.LinkBaseUrl = linkBaseUrl;
        harness.Install(this);

        var stub = new StubApi(status, body);
        Services.RemoveAll<QikLogApiClient>();
        Services.AddSingleton(new QikLogApiClient(new HttpClient(stub) { BaseAddress = new Uri("http://api.test/") }));
        return stub;
    }

    [Fact]
    public void Given_a_live_link_When_the_page_renders_Then_it_shows_the_address_the_snippet_the_warning_and_stored_lines()
    {
        // Given: the API has one stored line for this link
        var stub = Install(
            HttpStatusCode.OK,
            """[{"source":"link-k7mq2x9d4fab","level":"error","message":"it broke","timestamp":"2026-11-21T20:59:03Z"}]""");

        // When
        var cut = RenderComponent<Link>(parameters => parameters.Add(p => p.Id, LinkId));

        // Then: one address to send to and open, and the line you paste
        cut.Find("[data-link-url]").TextContent.ShouldBe($"https://qiklog.com/l/{LinkId}");
        cut.Find("[data-link-sample]").TextContent.ShouldContain($"curl https://qiklog.com/l/{LinkId} -d \"it broke\"");

        // Then: the honest notes about an unlisted link
        cut.Markup.ShouldContain("Anyone with this link can read it, so do not log secrets.");
        cut.Markup.ShouldContain("It lasts 24 hours and holds 500 lines.");
        cut.Find("a[href='/signup']").TextContent.ShouldContain("Create a free account");

        // Then: history comes from the link endpoint, not a tenant source
        stub.Paths.ShouldContain(path => path.StartsWith($"/l/{LinkId}/lines"));
        stub.Paths.ShouldNotContain(path => path.StartsWith("/v1/"));
        cut.Markup.ShouldContain("it broke");
        cut.Markup.ShouldContain("ERROR");

        // Then: the heading is plain words, not the internal source name
        cut.Markup.ShouldContain("Live tail");
        cut.Markup.ShouldNotContain($"link-{LinkId}");

        // Then: rows use the classes that let the message drop to its own line on a phone
        cut.Find(".ql-tail-row .ql-tail-msg").TextContent.ShouldBe("it broke");
        cut.Find("[data-link-sample]").ClassList.ShouldContain("ql-link-sample");
    }

    [Fact]
    public void Given_a_viewer_five_hours_behind_utc_When_the_tail_renders_Then_times_are_in_their_local_time()
    {
        // Given: a line stored at 20:59:03 UTC, and a browser in New York in winter
        Install(
            HttpStatusCode.OK,
            """[{"source":"link-k7mq2x9d4fab","level":"info","message":"it broke","timestamp":"2026-11-21T20:59:03Z"}]""");
        JSInterop.Setup<int>("qiklogTimezoneOffset").SetResult(300);

        // When
        var cut = RenderComponent<Link>(parameters => parameters.Add(p => p.Id, LinkId));

        // Then: 20:59 UTC reads as 15:59
        cut.WaitForAssertion(() => cut.Find(".ql-tail-ts").TextContent.ShouldBe("15:59:03.000"));
    }

    [Fact]
    public void Given_a_browser_that_reports_no_offset_When_the_tail_renders_Then_times_stay_utc()
    {
        // Given: no script answer (the test default returns 0)
        Install(
            HttpStatusCode.OK,
            """[{"source":"link-k7mq2x9d4fab","level":"info","message":"it broke","timestamp":"2026-11-21T20:59:03Z"}]""");

        // When
        var cut = RenderComponent<Link>(parameters => parameters.Add(p => p.Id, LinkId));

        // Then
        cut.Find(".ql-tail-ts").TextContent.ShouldBe("20:59:03.000");
    }

    [Fact]
    public void Given_configured_keys_When_the_link_page_renders_Then_markup_has_no_key()
    {
        // Given: both server credentials are set
        Install(HttpStatusCode.OK, "[]");

        // When
        var cut = RenderComponent<Link>(parameters => parameters.Add(p => p.Id, LinkId));

        // Then: neither reaches the browser
        cut.Markup.ShouldNotContain(DemoKey);
        cut.Markup.ShouldNotContain(HubKey);
        cut.Markup.ShouldContain("Waiting for your first line...");
    }

    [Fact]
    public void Given_no_public_origin_configured_When_the_page_renders_Then_the_api_origin_is_shown()
    {
        // Given: QikLog:LinkBaseUrl is unset and the API is at api.test
        var harness = new PlaygroundHarness(DemoKey, HubKey);
        harness.Install(this);
        Services.RemoveAll<QikLogApiClient>();
        Services.AddSingleton(new QikLogApiClient(
            new HttpClient(new StubApi(HttpStatusCode.OK, "[]")) { BaseAddress = new Uri("http://api.test/") }));
        harness.Options.ApiBaseUrl = "";
        harness.Options.LinkBaseUrl = null;

        // When
        var cut = RenderComponent<Link>(parameters => parameters.Add(p => p.Id, LinkId));

        // Then: the address falls back to the API base (blank here, so a relative address)
        cut.Find("[data-link-url]").TextContent.ShouldBe($"/l/{LinkId}");
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Gone)]
    public void Given_an_unknown_or_expired_link_When_the_page_renders_Then_it_says_so_and_offers_a_new_one(HttpStatusCode status)
    {
        // Given
        Install(status, """{"error":"gone"}""");

        // When
        var cut = RenderComponent<Link>(parameters => parameters.Add(p => p.Id, LinkId));

        // Then
        cut.Markup.ShouldContain("This log link is gone");
        cut.Find("a[href='https://qiklog.com']").TextContent.ShouldBe("Get a new log link");
        cut.FindAll("[data-link-url]").ShouldBeEmpty();
    }

    [Fact]
    public void Given_a_malformed_id_When_the_page_renders_Then_it_is_gone_without_calling_the_api()
    {
        // Given
        var stub = Install(HttpStatusCode.OK, "[]");

        // When
        var cut = RenderComponent<Link>(parameters => parameters.Add(p => p.Id, "../../etc/passwd"));

        // Then
        cut.Markup.ShouldContain("This log link is gone");
        stub.Paths.ShouldBeEmpty();
    }

    [Fact]
    public void Given_a_link_When_the_embed_renders_Then_it_is_compact_and_reads_the_link()
    {
        // Given
        var stub = Install(HttpStatusCode.OK, "[]");

        // When
        var cut = RenderComponent<EmbedLink>(parameters => parameters.Add(p => p.Id, LinkId));

        // Then
        cut.Markup.ShouldContain("ql-live-tail-compact");
        cut.Markup.ShouldContain("Waiting for your first line...");
        stub.Paths.ShouldContain(path => path.StartsWith($"/l/{LinkId}/lines"));
        cut.Markup.ShouldNotContain(DemoKey);
    }
}
