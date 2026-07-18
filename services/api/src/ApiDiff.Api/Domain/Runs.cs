namespace ApiDiff.Api.Domain;

/// <summary>One execution of a regression test against a pull-request build.</summary>
public class RegressionRun
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    /// <summary>Pull request number that triggered the run.</summary>
    public int PullRequestNumber { get; set; }

    /// <summary>Candidate commit SHA under test.</summary>
    public string CommitSha { get; set; } = null!;

    public RunStatus Status { get; set; } = RunStatus.Pending;

    /// <summary>Base URL of the ephemeral candidate environment (once provisioned).</summary>
    public string? CandidateBaseUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public ICollection<ReplayResult> Results { get; set; } = new List<ReplayResult>();
}

/// <summary>The outcome of replaying one scenario within a run.</summary>
public class ReplayResult
{
    public Guid Id { get; set; }

    public Guid RunId { get; set; }
    public RegressionRun Run { get; set; } = null!;

    public Guid ScenarioId { get; set; }
    public Scenario Scenario { get; set; } = null!;

    public RunVerdict Verdict { get; set; }

    /// <summary>Structured diff between baseline and candidate responses, as JSON.</summary>
    public string DiffJson { get; set; } = "{}";

    public long BaselineLatencyMs { get; set; }
    public long CandidateLatencyMs { get; set; }

    /// <summary>candidate - baseline latency; positive means slower.</summary>
    public long LatencyDeltaMs { get; set; }

    public string? Error { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
