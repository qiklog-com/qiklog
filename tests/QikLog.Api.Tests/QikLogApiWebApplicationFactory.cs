using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QikLog.Infrastructure.Data;
using QikLog.Infrastructure.Tenants;

namespace QikLog.Api.Tests;

/// <summary>
/// In-memory API host for HTTP integration tests with auth enforcement enabled.
/// </summary>
public class QikLogApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "http://localhost:5081",
                ["QikLog:AuthEnforcement:Enabled"] = "true",
                ["QikLog:Ingest:RequireApiKey"] = "true",
                ["QikLog:Management:Enabled"] = "true"
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        ApiTestData.SeedPrimaryTenantAsync(scope.ServiceProvider).GetAwaiter().GetResult();
        return host;
    }
}
