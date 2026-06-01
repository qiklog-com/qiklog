using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using QikLog.Api.Auth;
using QikLog.Api.Auth.Testing;
using QikLog.Infrastructure.Auth;
using QikLog.Infrastructure.Tenants;

namespace QikLog.Api;

internal static class ApiAuthExtensions
{
    public static IServiceCollection AddQikLogJwtAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<AuthEnforcementOptions>(configuration.GetSection(AuthEnforcementOptions.SectionName));
        services.AddScoped<TenantResolver>();
        services.AddScoped<TenantAuthenticationService>();

        if (environment.IsEnvironment("Testing"))
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });
            services.AddAuthorization();
            return services;
        }

        var auth = configuration.GetSection(QikLogAuthOptions.SectionName).Get<QikLogAuthOptions>() ?? new();

        if (!auth.Enabled || string.IsNullOrWhiteSpace(auth.Authority))
            return services;

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = auth.Authority.TrimEnd('/');
                options.Audience = auth.ApiAudience;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    NameClaimType = "name"
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken)
                            && path.StartsWithSegments("/hubs/logs"))
                            context.Token = accessToken;

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }
}
