using ApiDiff.Api.Domain;

namespace ApiDiff.Api.Orchestration;

/// <summary>A provisioned ephemeral environment running the candidate build.</summary>
public sealed record EnvironmentHandle(string CandidateBaseUrl, string? Namespace);

/// <summary>Provisions and tears down the ephemeral per-PR environment (ADR 0004).</summary>
public interface IEnvironmentProvisioner
{
    Task<EnvironmentHandle> ProvisionAsync(RegressionRun run, Project project, CancellationToken ct);

    Task TeardownAsync(EnvironmentHandle handle, CancellationToken ct);
}

/// <summary>The outcome of replaying one scenario, mapped from the engine's result.</summary>
public sealed record ReplayOutcome(
    Guid ScenarioId,
    RunVerdict Verdict,
    string DiffJson,
    long BaselineLatencyMs,
    long CandidateLatencyMs,
    long LatencyDeltaMs,
    string? Error);

/// <summary>Calls the Go replay engine to replay scenarios against two targets.</summary>
public interface IReplayClient
{
    Task<IReadOnlyList<ReplayOutcome>> ReplayAsync(
        RegressionRun run,
        IReadOnlyList<Scenario> scenarios,
        string baselineUrl,
        string candidateUrl,
        CancellationToken ct);
}

/// <summary>Posts a pass/fail check back to GitHub for a run's commit.</summary>
public interface IGitHubChecks
{
    Task PostAsync(RegressionRun run, Project project, bool success, string summary, string detailsUrl, CancellationToken ct);
}

/// <summary>In-process queue of runs awaiting orchestration.</summary>
public interface IRunQueue
{
    void Enqueue(Guid runId);

    ValueTask<Guid> DequeueAsync(CancellationToken ct);
}

/// <summary>Drives a single run through the regression state machine.</summary>
public interface IRunOrchestrator
{
    Task ExecuteAsync(Guid runId, CancellationToken ct);
}
