using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiDiff.Api.Tests.Infrastructure;

/// <summary>
/// Test authentication scheme. A request is authenticated when it carries an
/// <c>X-Test-Sub</c> header, which becomes the subject claim; absent it, the
/// request is anonymous (so 401 paths can be exercised).
/// </summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string SubHeader = "X-Test-Sub";
    public const string EmailHeader = "X-Test-Email";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(SubHeader, out var sub) || string.IsNullOrEmpty(sub))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var email = Request.Headers.TryGetValue(EmailHeader, out var e) && !string.IsNullOrEmpty(e)
            ? e.ToString()
            : $"{sub}@test.local";

        var claims = new[]
        {
            new Claim("sub", sub.ToString()),
            new Claim("email", email),
            new Claim("name", email),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
