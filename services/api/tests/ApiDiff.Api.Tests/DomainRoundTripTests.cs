using ApiDiff.Api.Domain;
using ApiDiff.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace ApiDiff.Api.Tests;

/// <summary>
/// Integration test: the initial migration applies to a fresh Postgres and the
/// full domain graph round-trips. Satisfies the Phase 1 DoD.
/// Requires Docker (available on CI runners).
/// </summary>
public class DomainRoundTripTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    private ApiDiffDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApiDiffDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        return new ApiDiffDbContext(options);
    }

    [Fact]
    public async Task Migration_Applies_And_Graph_RoundTrips()
    {
        await using (var migrateCtx = NewContext())
        {
            await migrateCtx.Database.MigrateAsync();
        }

        var orgId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var scenarioId = Guid.NewGuid();
        var runId = Guid.NewGuid();

        await using (var write = NewContext())
        {
            var org = new Organization { Id = orgId, Name = "Acme", Slug = "acme", CreatedAt = DateTimeOffset.UtcNow };
            var project = new Project
            {
                Id = projectId,
                OrganizationId = orgId,
                Name = "Orders API",
                Slug = "orders",
                GitHubRepo = "acme/orders",
                BaselineBaseUrl = "https://baseline.acme.test",
                CreatedAt = DateTimeOffset.UtcNow,
            };
            var scenario = new Scenario
            {
                Id = scenarioId,
                ProjectId = projectId,
                Method = "GET",
                Path = "/v1/orders/123",
                Fingerprint = "abc123",
                ReferenceStatusCode = 200,
                CapturedAt = DateTimeOffset.UtcNow,
            };
            var run = new RegressionRun
            {
                Id = runId,
                ProjectId = projectId,
                PullRequestNumber = 42,
                CommitSha = "deadbeef",
                Status = RunStatus.Completed,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            var result = new ReplayResult
            {
                Id = Guid.NewGuid(),
                RunId = runId,
                ScenarioId = scenarioId,
                Verdict = RunVerdict.BehavioralRegression,
                DiffJson = """{"fields":[{"path":"total","kind":"CHANGED"}]}""",
                BaselineLatencyMs = 100,
                CandidateLatencyMs = 140,
                LatencyDeltaMs = 40,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            var audit = new AuditLog
            {
                OrganizationId = orgId,
                ActorUserId = null,
                Action = "run.completed",
                TargetType = "RegressionRun",
                TargetId = runId.ToString(),
                CreatedAt = DateTimeOffset.UtcNow,
            };

            write.AddRange(org, project, scenario, run, result, audit);
            await write.SaveChangesAsync();
        }

        await using var read = NewContext();

        var loadedProject = await read.Projects
            .Include(p => p.Scenarios)
            .Include(p => p.Runs).ThenInclude(r => r.Results)
            .SingleAsync(p => p.Id == projectId);

        Assert.Equal("orders", loadedProject.Slug);
        Assert.Single(loadedProject.Scenarios);
        var loadedRun = Assert.Single(loadedProject.Runs);
        Assert.Equal(RunStatus.Completed, loadedRun.Status);
        var loadedResult = Assert.Single(loadedRun.Results);
        Assert.Equal(RunVerdict.BehavioralRegression, loadedResult.Verdict);
        Assert.Equal(40, loadedResult.LatencyDeltaMs);

        Assert.Equal(1, await read.AuditLogs.CountAsync(a => a.OrganizationId == orgId));
    }
}
