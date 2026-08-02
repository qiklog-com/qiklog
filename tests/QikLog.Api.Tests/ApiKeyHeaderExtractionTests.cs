using Microsoft.AspNetCore.Http;
using QikLog.Api.Auth;
using QikLog.Core;
using Shouldly;
using Xunit;

namespace QikLog.Api.Tests;

public sealed class ApiKeyHeaderExtractionTests
{
    [Fact]
    public void TryGet_accepts_X_QikLog_API_Key()
    {
        var (plaintext, _) = ApiKeyFormat.Generate();
        var request = RequestWithHeader("X-QikLog-API-Key", plaintext);
        TenantAuthenticationService.TryGetApiKeyFromRequest(request, out var parsed).ShouldBeTrue();
        parsed.ShouldBe(plaintext);
    }

    [Fact]
    public void TryGet_accepts_legacy_X_Api_Key()
    {
        var (plaintext, _) = ApiKeyFormat.Generate();
        var request = RequestWithHeader("X-Api-Key", plaintext);
        TenantAuthenticationService.TryGetApiKeyFromRequest(request, out var parsed).ShouldBeTrue();
        parsed.ShouldBe(plaintext);
    }

    [Fact]
    public void TryGet_accepts_query_api_key()
    {
        var (plaintext, _) = ApiKeyFormat.Generate();
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString($"?api_key={plaintext}");
        TenantAuthenticationService.TryGetApiKeyFromRequest(context.Request, out var parsed).ShouldBeTrue();
        parsed.ShouldBe(plaintext);
    }

    [Fact]
    public void TryGet_accepts_Bearer_when_value_is_ql_key()
    {
        var (plaintext, _) = ApiKeyFormat.Generate();
        var request = RequestWithHeader("Authorization", $"Bearer {plaintext}");
        TenantAuthenticationService.TryGetApiKeyFromRequest(request, out var parsed).ShouldBeTrue();
        parsed.ShouldBe(plaintext);
    }

    [Fact]
    public void TryGet_rejects_Bearer_jwt_shaped_token()
    {
        var request = RequestWithHeader("Authorization", "Bearer eyJhbGciOiJIUzI1NiJ9.e30.sig");
        TenantAuthenticationService.TryGetApiKeyFromRequest(request, out _).ShouldBeFalse();
    }

    [Fact]
    public void TryGet_rejects_missing_headers()
    {
        var context = new DefaultHttpContext();
        TenantAuthenticationService.TryGetApiKeyFromRequest(context.Request, out _).ShouldBeFalse();
    }

    [Fact]
    public void TryGet_rejects_malformed_ql_key()
    {
        var request = RequestWithHeader("X-QikLog-API-Key", "ql_short");
        TenantAuthenticationService.TryGetApiKeyFromRequest(request, out _).ShouldBeFalse();
    }

    [Fact]
    public void TryGet_trims_whitespace_around_key()
    {
        var (plaintext, _) = ApiKeyFormat.Generate();
        var request = RequestWithHeader("X-QikLog-API-Key", $"  {plaintext}  ");
        TenantAuthenticationService.TryGetApiKeyFromRequest(request, out var parsed).ShouldBeTrue();
        parsed.ShouldBe(plaintext);
    }

    private static HttpRequest RequestWithHeader(string name, string value)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[name] = value;
        return context.Request;
    }
}
