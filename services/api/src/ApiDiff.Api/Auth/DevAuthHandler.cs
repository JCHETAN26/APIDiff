using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ApiDiff.Api.Auth;

/// <summary>
/// Development-only authentication: treats the bearer token string as the user's
/// subject, so the stack is usable locally with no identity provider. Enabled
/// only when <c>Authentication:DevMode</c> is true — never in production.
/// </summary>
public sealed class DevAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Dev";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? token = null;
        var header = Request.Headers.Authorization.ToString();
        if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = header["Bearer ".Length..].Trim();
        }

        if (string.IsNullOrEmpty(token))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[]
        {
            new Claim("sub", token),
            new Claim("email", $"{token}@dev.local"),
            new Claim("name", token),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
