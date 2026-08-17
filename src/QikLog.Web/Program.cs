using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.FluentUI.AspNetCore.Components;
using QikLog.Infrastructure.Auth;
using QikLog.Web;
using QikLog.Web.Components;
using QikLog.Web.Services;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    // Unencrypted key XML is expected in local/docker dev (no cert). Stale antiforgery cookies log once.
    builder.Logging.AddFilter("Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager", LogLevel.Error);
    builder.Logging.AddFilter("Microsoft.AspNetCore.Antiforgery", LogLevel.Warning);
}

// Blazor antiforgery + interactive circuits need stable keys across container restarts.
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "dataprotection-keys");
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("QikLog.Web");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAntiforgery(options =>
{
    // CSP frame-ancestors (set on AddInteractiveServerRenderMode) replaces XFO.
    options.SuppressXFrameOptionsHeader = true;
    // Cross-origin iframe (www → app) needs SameSite=None for the circuit cookie.
    // Secure+None is production-only; local HTTP iframes keep the default Lax cookie.
    if (!builder.Environment.IsDevelopment())
    {
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    }
});

builder.Services.AddFluentUIComponents();
builder.Services.AddHttpContextAccessor();
builder.Services.AddQikLogWebAuth(builder.Configuration, builder.Environment);

// Used by tail page to construct the SignalR hub URL.
// Override via appsettings or env var QIKLOG_API_BASE_URL in deployments.
builder.Services.Configure<QikLogOptions>(builder.Configuration.GetSection("QikLog"));

var apiBaseUrl = builder.Configuration["QikLog:ApiBaseUrl"] ?? "http://localhost:5080";
builder.Services.AddTransient<AccessTokenOrApiKeyHandler>();
builder.Services.AddHttpClient<QikLogApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
}).AddHttpMessageHandler<AccessTokenOrApiKeyHandler>();

var app = builder.Build();

// Behind TLS-terminating proxies (Railway, most PaaS) the container receives plain
// HTTP; without honoring X-Forwarded-Proto the OIDC redirect_uri is built as
// http:// and Zitadel refuses the callback. Must run before auth middleware.
// KnownNetworks/KnownProxies cleared because the platform proxy IP is not static.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();

var webAuth = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<QikLogAuthOptions>>().Value;
if (webAuth.Enabled && !string.IsNullOrWhiteSpace(webAuth.Authority))
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.UseAntiforgery();

if (webAuth.Enabled && !string.IsNullOrWhiteSpace(webAuth.Authority))
{
    app.MapGet("/challenge", () => Results.Challenge(
        new AuthenticationProperties { RedirectUri = "/" },
        [OpenIdConnectDefaults.AuthenticationScheme]));

    app.MapGet("/logout", async (HttpContext httpContext) =>
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await httpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme,
            new AuthenticationProperties { RedirectUri = "/" });
    });
}

// Marketing www iframes /embed/tail/{source}. Default is frame-ancestors 'self',
// which blocks www.qiklog.com. Policy is applied by interactive server render mode.
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode(options =>
    {
        options.ContentSecurityFrameAncestorsPolicy =
            "'self' https://www.qiklog.com https://qiklog.com http://localhost:4321 http://127.0.0.1:4321";
    });

app.Run();

public sealed class QikLogOptions
{
    public string ApiBaseUrl { get; set; } = "http://localhost:5080";

    /// <summary>Optional API key for SignalR hub + history when API auth enforcement is enabled.</summary>
    public string? HubApiKey { get; set; }
}
