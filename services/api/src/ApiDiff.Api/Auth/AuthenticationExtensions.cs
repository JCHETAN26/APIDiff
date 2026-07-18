using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace ApiDiff.Api.Auth;

/// <summary>Registers OIDC/JWT bearer authentication and authorization.</summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Adds JWT bearer auth configured from the "Authentication" section
    /// (Authority + Audience). In environments without an identity provider
    /// configured (e.g. local bring-up), the scheme is still registered but no
    /// tokens will validate — integration tests substitute a test scheme.
    /// </summary>
    public static IServiceCollection AddApiDiffAuth(this IServiceCollection services, IConfiguration config)
    {
        var authority = config["Authentication:Authority"];
        var audience = config["Authentication:Audience"];

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                // Keep the raw "sub"/"email" claim types instead of remapping to
                // the long WS-* URIs, so downstream code reads familiar names.
                options.MapInboundClaims = false;
                options.TokenValidationParameters.NameClaimType = "sub";
                options.RequireHttpsMetadata = authority?.StartsWith("https", StringComparison.OrdinalIgnoreCase) ?? false;
            });

        services.AddAuthorization();
        return services;
    }
}
