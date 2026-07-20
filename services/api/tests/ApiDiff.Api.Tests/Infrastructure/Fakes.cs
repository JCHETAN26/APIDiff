using System.Collections.Concurrent;
using ApiDiff.Api.Domain;
using ApiDiff.Api.Orchestration;

namespace ApiDiff.Api.Tests.Infrastructure;

/// <summary>Fake replay client whose per-run behavior tests control.</summary>
public sealed class FakeReplayClient : IReplayClient
{
    /// <summary>Maps the scenarios of a run to outcomes. Defaults to all-pass.</summary>
    public Func<IReadOnlyList<Scenario>, IReadOnlyList<ReplayOutcome>> Behavior { get; set; } =
        scenarios => scenarios
            .Select(s => new ReplayOutcome(s.Id, RunVerdict.Pass, "{}", 10, 10, 0, null))
            .ToList();

    public Task<IReadOnlyList<ReplayOutcome>> ReplayAsync(
        RegressionRun run, IReadOnlyList<Scenario> scenarios, string baselineUrl, string candidateUrl, CancellationToken ct)
        => Task.FromResult(Behavior(scenarios));
}

/// <summary>Fake provisioner that records provision/teardown counts.</summary>
public sealed class FakeEnvironmentProvisioner : IEnvironmentProvisioner
{
    private int _provisioned;
    private int _tornDown;

    public int Provisioned => Volatile.Read(ref _provisioned);
    public int TornDown => Volatile.Read(ref _tornDown);

    public Task<EnvironmentHandle> ProvisionAsync(RegressionRun run, Project project, CancellationToken ct)
    {
        Interlocked.Increment(ref _provisioned);
        return Task.FromResult(new EnvironmentHandle("http://candidate.test", $"pr-{run.PullRequestNumber}"));
    }

    public Task TeardownAsync(EnvironmentHandle handle, CancellationToken ct)
    {
        Interlocked.Increment(ref _tornDown);
        return Task.CompletedTask;
    }
}

/// <summary>Fake analysis client whose behavior tests control.</summary>
public sealed class FakeAnalysisClient : IAnalysisClient
{
    /// <summary>Maps failing outcomes to explanations. Defaults to one explanation covering all.</summary>
    public Func<IReadOnlyList<ReplayOutcome>, IReadOnlyList<ExplanationDto>> Behavior { get; set; } =
        failures => failures.Count == 0
            ? []
            : [new ExplanationDto("Failures explained", "detail", failures.Select(f => f.ScenarioId.ToString()).ToList(), 0.9, "request error")];

    public Task<IReadOnlyList<ExplanationDto>> ExplainAsync(
        string runId, IReadOnlyList<ReplayOutcome> failures, CancellationToken ct)
        => Task.FromResult(Behavior(failures));
}

public sealed record RecordedCheck(Guid RunId, bool Success, string Summary, string DetailsUrl);

/// <summary>Fake GitHub checks that records what would have been posted.</summary>
public sealed class RecordingGitHubChecks : IGitHubChecks
{
    private readonly ConcurrentBag<RecordedCheck> _checks = [];

    public IReadOnlyCollection<RecordedCheck> Checks => _checks.ToArray();

    public Task PostAsync(RegressionRun run, Project project, bool success, string summary, string detailsUrl, CancellationToken ct)
    {
        _checks.Add(new RecordedCheck(run.Id, success, summary, detailsUrl));
        return Task.CompletedTask;
    }
}
