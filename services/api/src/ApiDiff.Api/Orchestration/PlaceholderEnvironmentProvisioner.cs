using ApiDiff.Api.Domain;
using Microsoft.Extensions.Options;

namespace ApiDiff.Api.Orchestration;

/// <summary>
/// Placeholder provisioner: derives a candidate URL from configuration instead
/// of creating a real Kubernetes environment. The real GKE-backed provisioner
/// lands in Phase 8; the orchestration flow is identical either way.
/// </summary>
public sealed class PlaceholderEnvironmentProvisioner(
    IOptions<OrchestrationOptions> options,
    ILogger<PlaceholderEnvironmentProvisioner> logger) : IEnvironmentProvisioner
{
    public Task<EnvironmentHandle> ProvisionAsync(RegressionRun run, Project project, CancellationToken ct)
    {
        var url = options.Value.CandidateBaseUrlTemplate
            .Replace("{pr}", run.PullRequestNumber.ToString(), StringComparison.Ordinal);
        logger.LogInformation("Provisioning (placeholder) candidate env for run {RunId} at {Url}", run.Id, url);
        return Task.FromResult(new EnvironmentHandle(url, Namespace: $"pr-{run.PullRequestNumber}"));
    }

    public Task TeardownAsync(EnvironmentHandle handle, CancellationToken ct)
    {
        logger.LogInformation("Tearing down (placeholder) env {Namespace}", handle.Namespace);
        return Task.CompletedTask;
    }
}
