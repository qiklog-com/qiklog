using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QikLog.Web.Services;
using Shouldly;
using Xunit;

namespace QikLog.Web.Tests;

public sealed class AccessTokenOrApiKeyHandlerTests
{
    [Fact]
    public async Task Given_signed_in_user_without_saved_token_When_calling_api_Then_hub_key_is_not_sent()
    {
        // Given: an authenticated user whose auth ticket has no access_token
        var (handler, recorder) = CreateHandler(authenticated: true, accessToken: null, hubApiKey: "hub-key");

        // When: the web app calls the API on their behalf
        await Send(handler);

        // Then: neither the hub key header nor a Bearer is attached
        recorder.Last!.Headers.Contains("X-QikLog-API-Key").ShouldBeFalse();
        recorder.Last.Headers.Authorization.ShouldBeNull();
    }

    [Fact]
    public async Task Given_signed_in_user_with_token_When_calling_api_Then_bearer_is_their_token()
    {
        // Given: an authenticated user with a saved access token
        var (handler, recorder) = CreateHandler(authenticated: true, accessToken: "user-jwt", hubApiKey: "hub-key");

        // When: calling the API
        await Send(handler);

        // Then: Bearer user-jwt, no hub key
        recorder.Last!.Headers.Authorization!.Parameter.ShouldBe("user-jwt");
        recorder.Last.Headers.Contains("X-QikLog-API-Key").ShouldBeFalse();
    }

    [Fact]
    public async Task Given_anonymous_visitor_When_calling_api_Then_hub_key_is_sent()
    {
        // Given: anonymous visitor, hub key configured
        var (handler, recorder) = CreateHandler(authenticated: false, accessToken: null, hubApiKey: "hub-key");

        // When: calling the API (demo history)
        await Send(handler);

        // Then: hub key header
        recorder.Last!.Headers.GetValues("X-QikLog-API-Key").ShouldBe(["hub-key"]);
    }

    [Fact]
    public async Task Given_no_http_context_When_calling_api_Then_hub_key_is_not_sent()
    {
        // Given: a call from an interactive circuit where HttpContext is unavailable,
        // so we cannot tell whether the user is signed in
        var recorder = new RecordingHandler();
        var handler = new AccessTokenOrApiKeyHandler(
            new HttpContextAccessor { HttpContext = null },
            Options.Create(new QikLogOptions { HubApiKey = "hub-key" }))
        {
            InnerHandler = recorder
        };

        // When: calling the API
        await Send(handler);

        // Then: fail closed, no shared credential
        recorder.Last!.Headers.Contains("X-QikLog-API-Key").ShouldBeFalse();
        recorder.Last.Headers.Authorization.ShouldBeNull();
    }

    private static async Task Send(HttpMessageHandler handler)
    {
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.test/v1/sources");
        using var _ = await invoker.SendAsync(request, CancellationToken.None);
    }

    private static (AccessTokenOrApiKeyHandler Handler, RecordingHandler Recorder) CreateHandler(
        bool authenticated,
        string? accessToken,
        string? hubApiKey)
    {
        var services = new ServiceCollection()
            .AddSingleton<IAuthenticationService>(new FakeAuthenticationService(accessToken))
            .BuildServiceProvider();

        var identity = authenticated
            ? new ClaimsIdentity([new Claim("sub", "user-1")], "test")
            : new ClaimsIdentity();
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity),
            RequestServices = services
        };

        var recorder = new RecordingHandler();
        var handler = new AccessTokenOrApiKeyHandler(
            new HttpContextAccessor { HttpContext = httpContext },
            Options.Create(new QikLogOptions { HubApiKey = hubApiKey }))
        {
            InnerHandler = recorder
        };
        return (handler, recorder);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Last { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Last = request;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }

    private sealed class FakeAuthenticationService(string? accessToken) : IAuthenticationService
    {
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
        {
            var properties = new AuthenticationProperties();
            if (accessToken is not null)
                properties.StoreTokens([new AuthenticationToken { Name = "access_token", Value = accessToken }]);

            var ticket = new AuthenticationTicket(context.User, properties, "test");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) => Task.CompletedTask;

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
    }
}
