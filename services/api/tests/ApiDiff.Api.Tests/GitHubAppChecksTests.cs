using System.Net;
using System.Security.Cryptography;
using ApiDiff.Api.Domain;
using ApiDiff.Api.Orchestration;
using ApiDiff.Api.Orchestration.GitHub;
using ApiDiff.Api.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace ApiDiff.Api.Tests;

public class GitHubAppChecksTests
{
    private sealed class SingleClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private static string Pem()
    {
        using var rsa = RSA.Create(2048);
        return rsa.ExportRSAPrivateKeyPem();
    }

    [Fact]
    public async Task PostAsync_ResolvesInstallation_ExchangesToken_PostsCheckRun()
    {
        var handler = new StubHttpHandler()
            .On("GET", "/repos/acme/orders/installation", HttpStatusCode.OK, """{"id":999}""")
            .On("POST", "/app/installations/999/access_tokens", HttpStatusCode.Created, """{"token":"ghs_installtoken"}""")
            .On("POST", "/repos/acme/orders/check-runs", HttpStatusCode.Created, """{"id":1}""");

        var client = new HttpClient(handler);
        var options = Options.Create(new GitHubOptions
        {
            AppId = "42",
            PrivateKeyPem = Pem(),
            ApiBaseUrl = "https://api.github.test",
        });

        var checks = new GitHubAppChecks(
            new SingleClientFactory(client), options, NullLogger<GitHubAppChecks>.Instance, new FakeTimeProvider());

        var run = new RegressionRun { Id = Guid.NewGuid(), CommitSha = "deadbeef", PullRequestNumber = 3 };
        var project = new Project { GitHubRepo = "acme/orders" };

        await checks.PostAsync(run, project, success: false, "1 of 2 scenarios regressed.", "https://dash/runs/x", CancellationToken.None);

        // Three calls in order: installation lookup, token exchange, check-run.
        Assert.Equal(3, handler.Requests.Count);
        Assert.EndsWith("/repos/acme/orders/check-runs", handler.Requests[2].RequestUri!.AbsolutePath);

        // The check-run carries the head sha and a failure conclusion.
        var checkRunBody = handler.Bodies[2];
        Assert.Contains("\"head_sha\":\"deadbeef\"", checkRunBody);
        Assert.Contains("\"conclusion\":\"failure\"", checkRunBody);
        Assert.Contains("regressed", checkRunBody);

        // The installation lookup used the App JWT; the check-run used the installation token.
        Assert.Equal("Bearer", handler.Requests[0].Headers.Authorization!.Scheme);
        Assert.Equal("ghs_installtoken", handler.Requests[2].Headers.Authorization!.Parameter);
    }

    [Fact]
    public async Task PostAsync_UsesConfiguredInstallationId_SkippingLookup()
    {
        var handler = new StubHttpHandler()
            .On("POST", "/app/installations/555/access_tokens", HttpStatusCode.Created, """{"token":"t"}""")
            .On("POST", "/repos/acme/orders/check-runs", HttpStatusCode.Created, """{"id":1}""");

        var options = Options.Create(new GitHubOptions
        {
            AppId = "42",
            PrivateKeyPem = Pem(),
            InstallationId = 555,
        });

        var checks = new GitHubAppChecks(
            new SingleClientFactory(new HttpClient(handler)), options, NullLogger<GitHubAppChecks>.Instance, new FakeTimeProvider());

        await checks.PostAsync(
            new RegressionRun { CommitSha = "abc", PullRequestNumber = 1 },
            new Project { GitHubRepo = "acme/orders" },
            success: true, "All passed.", "https://dash", CancellationToken.None);

        // No installation lookup; token exchange then check-run.
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("/app/installations/555/access_tokens", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Contains("\"conclusion\":\"success\"", handler.Bodies[1]);
    }
}
