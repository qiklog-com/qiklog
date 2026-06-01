using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QikLog.Infrastructure.Auth;
using QikLog.Infrastructure.Data;
using QikLog.Infrastructure.Tenants;

namespace QikLog.Web;

internal static class WebAuthExtensions
{
    public static IServiceCollection AddQikLogWebAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<QikLogAuthOptions>(configuration.GetSection(QikLogAuthOptions.SectionName));
        var auth = configuration.GetSection(QikLogAuthOptions.SectionName).Get<QikLogAuthOptions>() ?? new();

        if (!auth.Enabled || string.IsNullOrWhiteSpace(auth.Authority))
            return services;

        var connectionString = configuration.GetConnectionString("Postgres");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<QikLogDbContext>(options =>
                options.UseNpgsql(connectionString));
        }
        else if (environment.IsDevelopment())
        {
            services.AddDbContext<QikLogDbContext>(options =>
                options.UseInMemoryDatabase("QikLogWebAuth"));
        }

        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<TenantProvisioner>();

        services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddOpenIdConnect(options =>
            {
                options.Authority = auth.Authority.TrimEnd('/');
                options.ClientId = auth.ClientId;
                options.ClientSecret = auth.ClientSecret;
                options.ResponseType = "code";
                options.SaveTokens = true;
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");

                options.Events.OnTokenValidated = async context =>
                {
                    var orgId = context.Principal?.FindFirstValue(auth.OrganizationClaim);
                    var name = context.Principal?.FindFirstValue("name")
                        ?? context.Principal?.FindFirstValue(ClaimTypes.Email)
                        ?? "Tenant";

                    await using var scope = context.HttpContext.RequestServices.CreateAsyncScope();
                    var provisioner = scope.ServiceProvider.GetRequiredService<TenantProvisioner>();
                    var tenantId = await provisioner.EnsureTenantAsync(
                        orgId,
                        name,
                        context.HttpContext.RequestAborted);

                    var identity = (ClaimsIdentity)context.Principal!.Identity!;
                    identity.AddClaim(new Claim("tenant_id", tenantId.ToString()));
                };
            });

        services.AddAuthorization();
        services.AddCascadingAuthenticationState();
        return services;
    }
}
