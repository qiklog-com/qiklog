using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.FluentUI.AspNetCore.Components;
using QikLog.Infrastructure.Auth;
using QikLog.Web;
using QikLog.Web.Components;

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

builder.Services.AddFluentUIComponents();
builder.Services.AddQikLogWebAuth(builder.Configuration, builder.Environment);

// Used by tail page to construct the SignalR hub URL.
// Override via appsettings or env var QIKLOG_API_BASE_URL in deployments.
builder.Services.Configure<QikLogOptions>(builder.Configuration.GetSection("QikLog"));

var apiBaseUrl = builder.Configuration["QikLog:ApiBaseUrl"] ?? "http://localhost:5080";
var hubApiKey = builder.Configuration["QikLog:HubApiKey"];
builder.Services.AddHttpClient<QikLog.Web.Services.QikLogApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    if (!string.IsNullOrWhiteSpace(hubApiKey))
        client.DefaultRequestHeaders.Add("X-QikLog-API-Key", hubApiKey);
});

var app = builder.Build();

// Behind TLS-terminating proxies (Railway, most PaaS) the container receives plain
// HTTP; without honoring X-Forwarded-Proto the OIDC redirect_uri is built as
// http:// and Zitadel refuses the callback. Must run before auth middleware.
// KnownNetworks/KnownProxies cleared because the platform proxy IP is not static.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor,
    KnownNetworks = { },
    KnownProxies = { }
});

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

app.MapGet("/challenge", () => Results.Challenge(
    new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = "/" },
    [Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme]));

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.Run();

public sealed class QikLogOptions
{
    public string ApiBaseUrl { get; set; } = "http://localhost:5080";

    /// <summary>Optional API key for SignalR hub + history when API auth enforcement is enabled.</summary>
    public string? HubApiKey { get; set; }
}
