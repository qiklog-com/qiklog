using Microsoft.AspNetCore.Http;
using QikLog.Api.Auth;
using Shouldly;
using Xunit;

namespace QikLog.Api.Tests;

public sealed class ProtectedApiRoutesTests
{
    [Theory]
    [InlineData("/v1/logs", "POST", AuthMode.ApiKey, true)]
    [InlineData("/v1/sources/demo/logs", "GET", AuthMode.JwtOrApiKey, true)]
    [InlineData("/v1/keys", "GET", AuthMode.Jwt, true)]
    [InlineData("/v1/keys", "POST", AuthMode.Jwt, true)]
    [InlineData("/v1/sources", "GET", AuthMode.Jwt, true)]
    [InlineData("/v1/billing/checkout", "POST", AuthMode.Jwt, true)]
    [InlineData("/v1/dev/keys", "POST", AuthMode.Jwt, true)]
    [InlineData("/hubs/logs", "GET", AuthMode.JwtOrApiKey, true)]
    [InlineData("/v1/logs", "GET", AuthMode.None, false)]
    [InlineData("/healthz", "GET", AuthMode.None, false)]
    public void TryGetAuthMode_maps_path_and_method(string path, string method, AuthMode expected, bool isProtected)
    {
        var found = ProtectedApiRoutes.TryGetAuthMode(new PathString(path), method, out var mode);
        found.ShouldBe(isProtected);
        mode.ShouldBe(expected);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/healthz")]
    [InlineData("/metrics")]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar/v1")]
    public void IsPublic_true_for_observability_and_docs(string path)
    {
        ProtectedApiRoutes.IsPublic(new PathString(path)).ShouldBeTrue();
    }

    [Theory]
    [InlineData("/v1/logs")]
    [InlineData("/v1/keys")]
    [InlineData("/tail/demo")]
    public void IsPublic_false_for_product_routes(string path)
    {
        ProtectedApiRoutes.IsPublic(new PathString(path)).ShouldBeFalse();
    }
}
