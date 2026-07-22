using System.Security.Claims;
using System.Text.Encodings.Web;
using ApiDiff.Api.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ApiDiff.Api.Tests;

public class DevAuthHandlerTests
{
    private sealed class StubMonitor : IOptionsMonitor<AuthenticationSchemeOptions>
    {
        private readonly AuthenticationSchemeOptions _options = new();
        public AuthenticationSchemeOptions CurrentValue => _options;
        public AuthenticationSchemeOptions Get(string? name) => _options;
        public IDisposable OnChange(Action<AuthenticationSchemeOptions, string?> listener) => new Noop();
        private sealed class Noop : IDisposable { public void Dispose() { } }
    }

    private static async Task<AuthenticateResult> AuthenticateAsync(string? authorizationHeader)
    {
        var handler = new DevAuthHandler(new StubMonitor(), NullLoggerFactory.Instance, UrlEncoder.Default);
        var context = new DefaultHttpContext();
        if (authorizationHeader is not null)
        {
            context.Request.Headers.Authorization = authorizationHeader;
        }

        await handler.InitializeAsync(
            new AuthenticationScheme(DevAuthHandler.SchemeName, null, typeof(DevAuthHandler)), context);
        return await handler.AuthenticateAsync();
    }

    [Fact]
    public async Task BearerToken_BecomesSubject()
    {
        var result = await AuthenticateAsync("Bearer alice");

        Assert.True(result.Succeeded);
        Assert.Equal("alice", result.Principal!.FindFirstValue("sub"));
        Assert.Equal("alice@dev.local", result.Principal!.FindFirstValue("email"));
    }

    [Fact]
    public async Task NoHeader_IsNoResult()
    {
        var result = await AuthenticateAsync(null);
        Assert.False(result.Succeeded);
        Assert.Null(result.Failure);
    }

    [Fact]
    public async Task EmptyBearer_IsNoResult()
    {
        var result = await AuthenticateAsync("Bearer ");
        Assert.False(result.Succeeded);
    }
}
