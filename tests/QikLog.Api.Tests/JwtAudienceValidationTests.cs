using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using QikLog.Infrastructure.Auth;
using Shouldly;
using Xunit;

namespace QikLog.Api.Tests;

/// <summary>
/// Fine-grained net for API JWT audience checks. Mirrors
/// <c>ApiAuthExtensions</c>: ValidateAudience = true, no wildcard, no custom bypass.
/// </summary>
public sealed class JwtAudienceValidationTests
{
    private static readonly SymmetricSecurityKey SigningKey =
        new(Encoding.UTF8.GetBytes("0123456789abcdef0123456789abcdef"));

    private const string Issuer = "https://qiklog-prod-bnimdu.us1.zitadel.cloud";

    [Fact]
    public void Default_api_audience_is_the_zitadel_project_id()
    {
        new QikLogAuthOptions().ApiAudience.ShouldBe("383416044909259568");
    }

    [Fact]
    public void Correct_audience_is_accepted()
    {
        var jwt = Issue(audience: "383416044909259568");
        Validate(jwt, out var principal);
        principal.ShouldNotBeNull();
        principal!.Identity!.IsAuthenticated.ShouldBeTrue();
    }

    [Fact]
    public void Wrong_audience_is_rejected()
    {
        var jwt = Issue(audience: "qiklog-api");
        Should.Throw<SecurityTokenInvalidAudienceException>(() => Validate(jwt, out _));
    }

    [Fact]
    public void Missing_audience_is_rejected()
    {
        var jwt = Issue(audience: null);
        Should.Throw<SecurityTokenException>(() => Validate(jwt, out _));
    }

    [Fact]
    public void Malformed_token_is_rejected()
    {
        Should.Throw<ArgumentException>(() => Validate("not-a-jwt", out _));
    }

    [Fact]
    public void Validation_parameters_do_not_bypass_audience()
    {
        var parameters = ProductionLikeParameters();
        parameters.ValidateAudience.ShouldBeTrue();
        parameters.AudienceValidator.ShouldBeNull();
        parameters.ValidAudience.ShouldBe("383416044909259568");
    }

    private static string Issue(string? audience)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: audience,
            claims: [new Claim("sub", "user-1")],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256));
        return handler.WriteToken(token);
    }

    private static void Validate(string jwt, out ClaimsPrincipal? principal)
    {
        var handler = new JwtSecurityTokenHandler();
        principal = handler.ValidateToken(jwt, ProductionLikeParameters(), out _);
    }

    private static TokenValidationParameters ProductionLikeParameters() => new()
    {
        ValidateIssuer = true,
        ValidIssuer = Issuer,
        ValidateAudience = true,
        ValidAudience = new QikLogAuthOptions().ApiAudience,
        ValidateLifetime = true,
        RequireExpirationTime = true,
        IssuerSigningKey = SigningKey,
        ClockSkew = TimeSpan.Zero,
        NameClaimType = "name"
    };
}
