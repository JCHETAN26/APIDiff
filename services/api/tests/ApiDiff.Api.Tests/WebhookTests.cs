using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using ApiDiff.Api.Domain;
using ApiDiff.Api.Persistence;
using ApiDiff.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ApiDiff.Api.Tests;

public class WebhookTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static string Sign(string secret, string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return "sha256=" + Convert.ToHexStringLower(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }

    private static string PrPayload(string action, string repo, int number, string sha) =>
        $"{{\"action\":\"{action}\",\"number\":{number}," +
        $"\"pull_request\":{{\"head\":{{\"sha\":\"{sha}\"}}}}," +
        $"\"repository\":{{\"full_name\":\"{repo}\"}}}}";

    private async Task<HttpResponseMessage> PostAsync(string eventType, string payload, string? secret = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/github")
        {
            Content = new StringContent(payload, Encoding.UTF8),
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Headers.Add("X-GitHub-Event", eventType);
        request.Headers.Add("X-GitHub-Delivery", Guid.NewGuid().ToString());
        request.Headers.Add("X-Hub-Signature-256", Sign(secret ?? ApiFactory.WebhookSecret, payload));
        return await factory.CreateClient().SendAsync(request);
    }

    private async Task<(Guid orgId, Guid projectId, string repo)> SeedProjectAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiDiffDbContext>();
        var orgId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var repo = "acme/repo-" + Guid.NewGuid().ToString("N")[..10];
        db.Organizations.Add(new Organization { Id = orgId, Name = "Acme", Slug = "acme-" + Guid.NewGuid().ToString("N")[..8], CreatedAt = DateTimeOffset.UtcNow });
        db.Projects.Add(new Project
        {
            Id = projectId,
            OrganizationId = orgId,
            Name = "Repo",
            Slug = "repo-" + Guid.NewGuid().ToString("N")[..8],
            GitHubRepo = repo,
            BaselineBaseUrl = "https://baseline.test",
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
        return (orgId, projectId, repo);
    }

    private async Task<RegressionRun?> WaitForRunAsync(Guid projectId, string sha, RunStatus status)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < TimeSpan.FromSeconds(15))
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApiDiffDbContext>();
            var run = await db.RegressionRuns.FirstOrDefaultAsync(r => r.ProjectId == projectId && r.CommitSha == sha);
            if (run is not null && run.Status == status)
            {
                return run;
            }

            await Task.Delay(100);
        }

        return null;
    }

    [Fact]
    public async Task InvalidSignature_Returns401()
    {
        var payload = PrPayload("opened", "acme/x", 1, "sha1");
        var response = await PostAsync("pull_request", payload, secret: "wrong-secret");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task NonPullRequestEvent_Ignored()
    {
        var response = await PostAsync("push", """{"ref":"refs/heads/main"}""");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task IgnoredAction_NoContent()
    {
        var (_, _, repo) = await SeedProjectAsync();
        var response = await PostAsync("pull_request", PrPayload("closed", repo, 1, "sha1"));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UnknownRepo_NoContent()
    {
        var response = await PostAsync("pull_request", PrPayload("opened", "nobody/nothing", 1, "sha1"));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PullRequestOpened_TriggersRun_AndPostsCheck()
    {
        var checks = factory.Services.GetRequiredService<RecordingGitHubChecks>();
        var (_, projectId, repo) = await SeedProjectAsync();
        const string sha = "deadbeefcafe";

        var response = await PostAsync("pull_request", PrPayload("opened", repo, 42, sha));
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var run = await WaitForRunAsync(projectId, sha, RunStatus.Completed);
        Assert.NotNull(run);
        Assert.Equal(42, run!.PullRequestNumber);

        var check = Assert.Single(checks.Checks, c => c.RunId == run.Id);
        Assert.Contains($"/runs/{run.Id}", check.DetailsUrl);
    }

    [Fact]
    public async Task DuplicateDelivery_IsIdempotent()
    {
        var (_, projectId, repo) = await SeedProjectAsync();
        const string sha = "idempotent123";

        var first = await PostAsync("pull_request", PrPayload("synchronize", repo, 5, sha));
        var second = await PostAsync("pull_request", PrPayload("synchronize", repo, 5, sha));
        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, second.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiDiffDbContext>();
        Assert.Equal(1, await db.RegressionRuns.CountAsync(r => r.ProjectId == projectId && r.CommitSha == sha));
    }
}
