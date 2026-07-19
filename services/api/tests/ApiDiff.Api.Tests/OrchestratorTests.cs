using ApiDiff.Api.Domain;
using ApiDiff.Api.Orchestration;
using ApiDiff.Api.Persistence;
using ApiDiff.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ApiDiff.Api.Tests;

public class OrchestratorTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private async Task<(Guid runId, Guid orgId, Guid[] scenarioIds)> SeedRunAsync(int scenarioCount)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiDiffDbContext>();

        var orgId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        db.Organizations.Add(new Organization { Id = orgId, Name = "Acme", Slug = "acme-" + Guid.NewGuid().ToString("N")[..8], CreatedAt = DateTimeOffset.UtcNow });
        db.Projects.Add(new Project
        {
            Id = projectId,
            OrganizationId = orgId,
            Name = "Orders",
            Slug = "orders-" + Guid.NewGuid().ToString("N")[..8],
            GitHubRepo = "acme/orders-" + Guid.NewGuid().ToString("N")[..8],
            BaselineBaseUrl = "https://baseline.test",
            CreatedAt = DateTimeOffset.UtcNow,
        });

        var scenarioIds = new Guid[scenarioCount];
        for (var i = 0; i < scenarioCount; i++)
        {
            scenarioIds[i] = Guid.NewGuid();
            db.Scenarios.Add(new Scenario
            {
                Id = scenarioIds[i],
                ProjectId = projectId,
                Method = "GET",
                Path = $"/v1/orders/{i}",
                Fingerprint = "fp-" + i,
                ReferenceStatusCode = 200,
                CapturedAt = DateTimeOffset.UtcNow,
            });
        }

        db.RegressionRuns.Add(new RegressionRun
        {
            Id = runId,
            ProjectId = projectId,
            PullRequestNumber = 7,
            CommitSha = "abc123",
            Status = RunStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
        return (runId, orgId, scenarioIds);
    }

    private async Task ExecuteAsync(Guid runId)
    {
        using var scope = factory.Services.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<IRunOrchestrator>();
        await orchestrator.ExecuteAsync(runId, CancellationToken.None);
    }

    [Fact]
    public async Task AllPass_CompletesWithSuccessCheck()
    {
        var checks = factory.Services.GetRequiredService<RecordingGitHubChecks>();
        var provisioner = factory.Services.GetRequiredService<FakeEnvironmentProvisioner>();
        var replay = factory.Services.GetRequiredService<FakeReplayClient>();
        replay.Behavior = s => s.Select(x => new ReplayOutcome(x.Id, RunVerdict.Pass, "{}", 10, 10, 0, null)).ToList();

        var (runId, _, _) = await SeedRunAsync(2);
        var teardownBefore = provisioner.TornDown;

        await ExecuteAsync(runId);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiDiffDbContext>();
        var run = await db.RegressionRuns.SingleAsync(r => r.Id == runId);
        Assert.Equal(RunStatus.Completed, run.Status);
        Assert.NotNull(run.CandidateBaseUrl);
        Assert.Equal(2, await db.ReplayResults.CountAsync(r => r.RunId == runId));
        Assert.True(await db.AuditLogs.AnyAsync(a => a.Action == "run.completed" && a.TargetId == runId.ToString()));

        var check = Assert.Single(checks.Checks, c => c.RunId == runId);
        Assert.True(check.Success);
        Assert.Contains($"/runs/{runId}", check.DetailsUrl);
        Assert.True(provisioner.TornDown > teardownBefore);
    }

    [Fact]
    public async Task Regression_CompletesWithFailureCheck()
    {
        var checks = factory.Services.GetRequiredService<RecordingGitHubChecks>();
        var replay = factory.Services.GetRequiredService<FakeReplayClient>();
        replay.Behavior = s => s.Select((x, i) => new ReplayOutcome(
            x.Id,
            i == 0 ? RunVerdict.BehavioralRegression : RunVerdict.Pass,
            """{"fields":[{"path":"total"}]}""", 10, 15, 5, null)).ToList();

        var (runId, _, _) = await SeedRunAsync(2);

        await ExecuteAsync(runId);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiDiffDbContext>();
        var run = await db.RegressionRuns.SingleAsync(r => r.Id == runId);
        Assert.Equal(RunStatus.Completed, run.Status);

        var check = Assert.Single(checks.Checks, c => c.RunId == runId);
        Assert.False(check.Success);
        Assert.Contains("regressed", check.Summary);
    }

    [Fact]
    public async Task ReplayThrows_MarksRunFailed_AndTearsDown()
    {
        var checks = factory.Services.GetRequiredService<RecordingGitHubChecks>();
        var provisioner = factory.Services.GetRequiredService<FakeEnvironmentProvisioner>();
        var replay = factory.Services.GetRequiredService<FakeReplayClient>();
        replay.Behavior = _ => throw new InvalidOperationException("engine unreachable");

        var (runId, _, _) = await SeedRunAsync(1);
        var teardownBefore = provisioner.TornDown;

        await ExecuteAsync(runId);
        replay.Behavior = s => s.Select(x => new ReplayOutcome(x.Id, RunVerdict.Pass, "{}", 10, 10, 0, null)).ToList();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiDiffDbContext>();
        var run = await db.RegressionRuns.SingleAsync(r => r.Id == runId);
        Assert.Equal(RunStatus.Failed, run.Status);
        Assert.True(await db.AuditLogs.AnyAsync(a => a.Action == "run.failed" && a.TargetId == runId.ToString()));

        var check = Assert.Single(checks.Checks, c => c.RunId == runId);
        Assert.False(check.Success);
        Assert.True(provisioner.TornDown > teardownBefore);
    }
}
