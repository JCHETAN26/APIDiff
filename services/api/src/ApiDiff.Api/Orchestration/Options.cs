namespace ApiDiff.Api.Orchestration;

/// <summary>GitHub integration settings (bound from the "GitHub" config section).</summary>
public sealed class GitHubOptions
{
    /// <summary>Shared secret used to verify webhook signatures.</summary>
    public string WebhookSecret { get; set; } = "";

    /// <summary>Token used to post commit statuses back to GitHub. When empty, checks are logged only.</summary>
    public string? Token { get; set; }

    /// <summary>GitHub REST API base (overridable for GitHub Enterprise / tests).</summary>
    public string ApiBaseUrl { get; set; } = "https://api.github.com";
}

/// <summary>Regression-run orchestration settings (bound from "Orchestration").</summary>
public sealed class OrchestrationOptions
{
    /// <summary>Address of the Go replay engine's gRPC server.</summary>
    public string ReplayEngineAddress { get; set; } = "http://replay-engine:9090";

    /// <summary>Base URL of the dashboard, used to build run report links.</summary>
    public string DashboardBaseUrl { get; set; } = "http://localhost:5173";

    /// <summary>Template for the ephemeral candidate environment URL; {pr} is substituted.</summary>
    public string CandidateBaseUrlTemplate { get; set; } = "http://pr-{pr}.candidate.svc.cluster.local";
}
