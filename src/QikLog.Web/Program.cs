using Microsoft.AspNetCore.DataProtection;
using Microsoft.FluentUI.AspNetCore.Components;
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

// Used by tail page to construct the SignalR hub URL.
// Override via appsettings or env var QIKLOG_API_BASE_URL in deployments.
builder.Services.Configure<QikLogOptions>(builder.Configuration.GetSection("QikLog"));

var apiBaseUrl = builder.Configuration["QikLog:ApiBaseUrl"] ?? "http://localhost:5080";
builder.Services.AddHttpClient<QikLog.Web.Services.QikLogApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.Run();

public sealed class QikLogOptions
{
    public string ApiBaseUrl { get; set; } = "http://localhost:5080";
}
