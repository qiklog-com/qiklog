using QikLog.Web.Services;
using Shouldly;
using Xunit;

namespace QikLog.Web.Tests;

public sealed class IngestSnippetTests
{
    [Fact]
    public void Given_live_api_url_When_building_curl_Then_snippet_targets_it_with_bearer_key()
    {
        // Given: the web app is configured for the production API
        // When: Manage builds the "source ready" snippet for source checkout
        var curl = IngestSnippet.Curl("https://api.qiklog.com/", "checkout");

        // Then: it posts to the live API with an Authorization header, not localhost
        curl.ShouldStartWith("curl -X POST https://api.qiklog.com/v1/logs");
        curl.ShouldContain("-H \"Authorization: Bearer $QIKLOG_API_KEY\"");
        curl.ShouldContain("\"source\":\"checkout\"");
        curl.ShouldNotContain("localhost");
    }

    [Fact]
    public void Given_source_with_quote_When_building_curl_Then_json_stays_valid()
    {
        // Given: a source name containing characters that would break the JSON body
        // When: building the snippet
        var curl = IngestSnippet.Curl("https://api.qiklog.com", "a\"b");

        // Then: the quote is escaped inside the JSON payload
        curl.ShouldContain("\"source\":\"a\\u0022b\"");
    }
}
