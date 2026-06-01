using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using QikLog.Infrastructure.Auth;

namespace QikLog.Api;

internal static class ApiAuthExtensions
{
    public static IServiceCollection AddQikLogJwtAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
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
            });

        services.AddAuthorization();
        return services;
    }
}
