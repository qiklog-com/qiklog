using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace QikLog.Api.Auth.Testing;

/// <summary>Test-only JWT substitute for integration tests (scheme <see cref="SchemeName"/>).</summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    /// <summary>Bearer token that authenticates as <see cref="TestTenants.Primary"/>.</summary>
    public const string ValidToken = "test-jwt-valid";

    /// <summary>Bearer token with a tenant id that does not exist in the database.</summary>
    public const string UnknownTenantToken = "test-jwt-unknown-tenant";

    /// <summary>Bearer token that fails authentication.</summary>
    public const string MalformedToken = "not-a-valid-test-token";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.Fail("expected Bearer token"));

        var token = header["Bearer ".Length..].Trim();
        if (token == MalformedToken)
            return Task.FromResult(AuthenticateResult.Fail("malformed test token"));

        if (token == ValidToken)
        {
            var identity = new ClaimsIdentity(
                [new Claim("tenant_id", TestTenants.Primary.ToString()), new Claim(ClaimTypes.Name, "test-user")],
                SchemeName);
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
        }

        if (token == UnknownTenantToken)
        {
            var identity = new ClaimsIdentity(
                [new Claim("tenant_id", TestTenants.Unknown.ToString())],
                SchemeName);
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
        }

        return Task.FromResult(AuthenticateResult.Fail("unknown test token"));
    }
}

/// <summary>Well-known tenant ids for test authentication.</summary>
public static class TestTenants
{
    public static readonly Guid Primary = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid Unknown = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
}
