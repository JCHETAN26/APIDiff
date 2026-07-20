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

    /// <summary>GitHub App id. When set with <see cref="PrivateKeyPem"/>, the App Checks API is used.</summary>
    public string? AppId { get; set; }

    /// <summary>PEM-encoded RSA private key for the GitHub App.</summary>
    public string? PrivateKeyPem { get; set; }

    /// <summary>Optional fixed installation id; when 0, it is resolved per repository.</summary>
    public long InstallationId { get; set; }

    /// <summary>True when the GitHub App credentials are configured.</summary>
    public bool UsesApp => !string.IsNullOrWhiteSpace(AppId) && !string.IsNullOrWhiteSpace(PrivateKeyPem);
}

/// <summary>Regression-run orchestration settings (bound from "Orchestration").</summary>
public sealed class OrchestrationOptions
{
    /// <summary>Address of the Go replay engine's gRPC server.</summary>
    public string ReplayEngineAddress { get; set; } = "http://replay-engine:9090";

    /// <summary>Address of the Python analysis service's gRPC server.</summary>
    public string AnalysisServiceAddress { get; set; } = "http://analysis:9091";

    /// <summary>Base URL of the dashboard, used to build run report links.</summary>
    public string DashboardBaseUrl { get; set; } = "http://localhost:5173";

    /// <summary>Template for the ephemeral candidate environment URL; {pr} is substituted.</summary>
    public string CandidateBaseUrlTemplate { get; set; } = "http://pr-{pr}.candidate.svc.cluster.local";

    /// <summary>Which provisioner to use: "placeholder" (default) or "kubernetes".</summary>
    public string Provisioner { get; set; } = "placeholder";
}

/// <summary>Settings for the Kubernetes ephemeral per-PR provisioner.</summary>
public sealed class KubernetesProvisionerOptions
{
    /// <summary>Namespace template for the per-PR environment; {pr} is substituted.</summary>
    public string NamespaceTemplate { get; set; } = "apidiff-pr-{pr}";

    /// <summary>Candidate container image template; {sha} and {pr} are substituted.</summary>
    public string CandidateImageTemplate { get; set; } = "candidate:{sha}";

    /// <summary>Port the candidate service listens on.</summary>
    public int CandidatePort { get; set; } = 8080;

    /// <summary>How long to wait for the candidate deployment to become available.</summary>
    public int ReadyTimeoutSeconds { get; set; } = 180;
}
