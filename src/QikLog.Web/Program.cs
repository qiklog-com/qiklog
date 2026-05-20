using Microsoft.FluentUI.AspNetCore.Components;
using QikLog.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddFluentUIComponents();

// Used by tail page to construct the SignalR hub URL.
// Override via appsettings or env var QIKLOG_API_BASE_URL in deployments.
builder.Services.Configure<QikLogOptions>(builder.Configuration.GetSection("QikLog"));

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
