using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
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

        // AuthorizeView in layouts/pages always needs cascading auth state, even when OIDC is off.
        services.AddAuthorization();
        services.AddCascadingAuthenticationState();

        if (!auth.Enabled || string.IsNullOrWhiteSpace(auth.Authority))
        {
            // Anonymous-only: satisfies AuthenticationStateProvider for AuthorizeView NotAuthorized paths.
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie();
            return services;
        }

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
            .AddCookie(options =>
            {
                options.Cookie.Name = "qiklog.auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.SlidingExpiration = true;
            })
            .AddOpenIdConnect(options =>
            {
                options.Authority = auth.Authority.TrimEnd('/');
                options.ClientId = auth.ClientId;
                options.ClientSecret = auth.ClientSecret;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.UsePkce = true;
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                // Keep Zitadel claim URIs (org id) instead of remapping to long ClaimTypes.* URIs.
                options.MapInboundClaims = false;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.Scope.Add("offline_access");
                options.Scope.Add(auth.ProjectAudienceScope);

                options.TokenValidationParameters.NameClaimType = "name";
                options.TokenValidationParameters.RoleClaimType = "roles";

                options.Events.OnTokenValidated = async context =>
                {
                    var orgId = context.Principal?.FindFirstValue(auth.OrganizationClaim)
                        ?? context.Principal?.FindFirstValue("urn:zitadel:iam:user:resourceowner:id");
                    var name = context.Principal?.FindFirstValue("name")
                        ?? context.Principal?.FindFirstValue("email")
                        ?? context.Principal?.FindFirstValue(ClaimTypes.Email)
                        ?? "Tenant";

                    await using var scope = context.HttpContext.RequestServices.CreateAsyncScope();
                    var provisioner = scope.ServiceProvider.GetRequiredService<TenantProvisioner>();
                    var tenantId = await provisioner.EnsureTenantAsync(
                        orgId,
                        name,
                        context.HttpContext.RequestAborted);

                    var identity = (ClaimsIdentity)context.Principal!.Identity!;
                    if (identity.FindFirst("tenant_id") is null)
                        identity.AddClaim(new Claim("tenant_id", tenantId.ToString()));
                };
            });

        return services;
    }
}
