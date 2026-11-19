using QikLog.Web.Services;
using Shouldly;
using Xunit;

namespace QikLog.Web.Tests;

/// <summary>
/// The shared hub API key belongs to the invite-beta bootstrap tenant. A signed-in
/// customer must never be sent to the API with it, or they would read that tenant's logs.
/// </summary>
public sealed class ApiCredentialPolicyTests
{
    [Fact]
    public void Given_signed_in_user_with_token_When_choosing_credential_Then_uses_their_token()
    {
        // Given: a signed-in user with a saved access token, and a hub key configured
        // When: choosing the API credential
        var credential = ApiCredentialPolicy.Choose(isAuthenticated: true, "user-jwt", "hub-key", isEmbed: false);

        // Then: their token
        credential.ShouldBe(ApiCredential.AccessToken("user-jwt"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Given_signed_in_user_without_token_When_choosing_credential_Then_hub_key_is_not_used(string? token)
    {
        // Given: a signed-in user whose access token is missing (expired cookie, not saved)
        // When: choosing the API credential with a hub key configured
        var credential = ApiCredentialPolicy.Choose(isAuthenticated: true, token, "hub-key", isEmbed: false);

        // Then: no credential; the API answers 401 and the UI asks them to sign in again
        credential.ShouldBe(ApiCredential.None);
    }

    [Fact]
    public void Given_anonymous_visitor_When_choosing_credential_Then_uses_hub_key_for_demo()
    {
        // Given: an anonymous visitor and a hub key configured
        // When: choosing the API credential
        var credential = ApiCredentialPolicy.Choose(isAuthenticated: false, null, " hub-key ", isEmbed: false);

        // Then: the demo hub key (trimmed)
        credential.ShouldBe(ApiCredential.HubApiKey("hub-key"));
    }

    [Fact]
    public void Given_anonymous_visitor_and_no_hub_key_When_choosing_credential_Then_none()
    {
        // Given/When: anonymous, nothing configured
        var credential = ApiCredentialPolicy.Choose(isAuthenticated: false, null, null, isEmbed: false);

        // Then: none
        credential.ShouldBe(ApiCredential.None);
    }

    [Fact]
    public void Given_marketing_embed_When_choosing_credential_Then_uses_hub_key_even_if_signed_in()
    {
        // Given: the www iframe embed, which always shows the demo source
        // When: a signed-in visitor loads it
        var credential = ApiCredentialPolicy.Choose(isAuthenticated: true, "user-jwt", "hub-key", isEmbed: true);

        // Then: hub key, never the visitor's own token
        credential.ShouldBe(ApiCredential.HubApiKey("hub-key"));
    }
}
