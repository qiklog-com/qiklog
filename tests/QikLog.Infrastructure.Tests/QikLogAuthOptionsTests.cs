using QikLog.Infrastructure.Auth;
using Shouldly;
using Xunit;

namespace QikLog.Infrastructure.Tests;

public sealed class QikLogAuthOptionsTests
{
    [Fact]
    public void ApiAudience_defaults_to_zitadel_project_id()
    {
        var options = new QikLogAuthOptions();
        options.ApiAudience.ShouldBe("383416044909259568");
        options.ProjectAudienceScope.ShouldBe(
            "urn:zitadel:iam:org:project:id:383416044909259568:aud");
    }

    [Fact]
    public void Ingest_options_defaults_are_unchanged_by_jwt_audience_work()
    {
        var ingest = new IngestAuthOptions();
        ingest.RequireApiKey.ShouldBeFalse();
        ingest.RateLimitPerMinute.ShouldBe(120);
        IngestAuthOptions.SectionName.ShouldBe("QikLog:Ingest");
    }
}
