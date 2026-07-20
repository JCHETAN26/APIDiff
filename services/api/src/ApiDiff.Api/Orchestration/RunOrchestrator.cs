using System.Text.Json;
using ApiDiff.Api.Auth;
using ApiDiff.Api.Domain;
using ApiDiff.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ApiDiff.Api.Orchestration;

/// <summary>
/// Drives a run through: provision → replay → analyze → complete, posting a
/// GitHub check and tearing down the environment. Status transitions and audit
/// entries are persisted at each step.
/// </summary>
public sealed class RunOrchestrator(
    ApiDiffDbContext db,
    IEnvironmentProvisioner provisioner,
    IReplayClient replayClient,
    IAnalysisClient analysisClient,
    IGitHubChecks githubChecks,
    IAuditService audit,
    IOptions<OrchestrationOptions> options,
    ILogger<RunOrchestrator> logger) : IRunOrchestrator
{
    public async Task ExecuteAsync(Guid runId, CancellationToken ct)
    {
        var run = await db.RegressionRuns.Include(r => r.Project).FirstOrDefaultAsync(r => r.Id == runId, ct);
        if (run is null)
        {
            logger.LogWarning("Run {RunId} not found; skipping", runId);
            return;
        }

        var project = run.Project;
        var detailsUrl = $"{options.Value.DashboardBaseUrl.TrimEnd('/')}/runs/{run.Id}";
        EnvironmentHandle? env = null;

        try
        {
            run.Status = RunStatus.Provisioning;
            run.StartedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);

            env = await provisioner.ProvisionAsync(run, project, ct);
            run.CandidateBaseUrl = env.CandidateBaseUrl;
            run.Status = RunStatus.Replaying;
            await db.SaveChangesAsync(ct);

            var scenarios = await db.Scenarios.Where(s => s.ProjectId == project.Id).ToListAsync(ct);
            var outcomes = await replayClient.ReplayAsync(run, scenarios, project.BaselineBaseUrl, env.CandidateBaseUrl, ct);

            foreach (var o in outcomes)
            {
                db.ReplayResults.Add(new ReplayResult
                {
                    Id = Guid.NewGuid(),
                    RunId = run.Id,
                    ScenarioId = o.ScenarioId,
                    Verdict = o.Verdict,
                    DiffJson = o.DiffJson,
                    BaselineLatencyMs = o.BaselineLatencyMs,
                    CandidateLatencyMs = o.CandidateLatencyMs,
                    LatencyDeltaMs = o.LatencyDeltaMs,
                    Error = o.Error,
                    CreatedAt = DateTimeOffset.UtcNow,
                });
            }

            run.Status = RunStatus.Analyzing;
            await db.SaveChangesAsync(ct);
            await AnalyzeAsync(run, outcomes, ct);

            var regressions = outcomes.Count(o => IsRegression(o.Verdict));
            var success = regressions == 0;

            run.Status = RunStatus.Completed;
            run.CompletedAt = DateTimeOffset.UtcNow;
            audit.Append(project.OrganizationId, null, "run.completed", nameof(RegressionRun), run.Id.ToString(),
                new { regressions, total = outcomes.Count });
            await db.SaveChangesAsync(ct);

            var summary = success
                ? $"All {outcomes.Count} scenarios passed."
                : $"{regressions} of {outcomes.Count} scenarios regressed.";
            await githubChecks.PostAsync(run, project, success, summary, detailsUrl, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Run {RunId} failed", runId);
            run.Status = RunStatus.Failed;
            run.CompletedAt = DateTimeOffset.UtcNow;
            audit.Append(project.OrganizationId, null, "run.failed", nameof(RegressionRun), run.Id.ToString(),
                new { error = ex.Message });
            await db.SaveChangesAsync(CancellationToken.None);

            try
            {
                await githubChecks.PostAsync(run, project, false, "Run failed: " + ex.Message, detailsUrl, CancellationToken.None);
            }
            catch (Exception postEx)
            {
                logger.LogError(postEx, "Failed to post failure check for run {RunId}", runId);
            }
        }
        finally
        {
            if (env is not null)
            {
                try
                {
                    await provisioner.TeardownAsync(env, CancellationToken.None);
                }
                catch (Exception teardownEx)
                {
                    logger.LogError(teardownEx, "Teardown failed for run {RunId}", runId);
                }
            }
        }
    }

    /// <summary>
    /// Best-effort: ask the analysis service to explain the failures and persist
    /// the ranked shortlist. Analysis errors never fail the run.
    /// </summary>
    private async Task AnalyzeAsync(RegressionRun run, IReadOnlyList<ReplayOutcome> outcomes, CancellationToken ct)
    {
        var failures = outcomes.Where(o => IsRegression(o.Verdict)).ToList();
        if (failures.Count == 0)
        {
            return;
        }

        try
        {
            var explanations = await analysisClient.ExplainAsync(run.Id.ToString(), failures, ct);
            foreach (var e in explanations)
            {
                db.RunExplanations.Add(new RunExplanation
                {
                    Id = Guid.NewGuid(),
                    RunId = run.Id,
                    Title = e.Title,
                    Detail = e.Detail,
                    ScenarioIdsJson = JsonSerializer.Serialize(e.ScenarioIds),
                    Severity = e.Severity,
                    LikelyCause = e.LikelyCause,
                    CreatedAt = DateTimeOffset.UtcNow,
                });
            }

            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Analysis failed for run {RunId}; continuing without explanations", run.Id);
        }
    }

    private static bool IsRegression(RunVerdict verdict) =>
        verdict is RunVerdict.BehavioralRegression or RunVerdict.PerfRegression or RunVerdict.Error;
}
