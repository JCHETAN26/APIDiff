using ApiDiff.Api.Domain;
using k8s;
using k8s.Autorest;
using Microsoft.Extensions.Options;

namespace ApiDiff.Api.Orchestration.Kubernetes;

/// <summary>
/// Provisions the ephemeral per-PR candidate environment on Kubernetes (ADR 0004):
/// a dedicated namespace with the candidate build's Deployment + Service, waited
/// until available. Teardown deletes the namespace, which cascades to everything
/// in it.
/// </summary>
public sealed class KubernetesEnvironmentProvisioner(
    IKubernetes client,
    IOptions<KubernetesProvisionerOptions> options,
    ILogger<KubernetesEnvironmentProvisioner> logger) : IEnvironmentProvisioner
{
    public async Task<EnvironmentHandle> ProvisionAsync(RegressionRun run, Project project, CancellationToken ct)
    {
        var opts = options.Value;
        var runId = run.Id.ToString();
        var ns = CandidateManifests.NamespaceName(opts.NamespaceTemplate, run.PullRequestNumber);
        var image = CandidateManifests.ImageReference(opts.CandidateImageTemplate, run.PullRequestNumber, run.CommitSha);

        logger.LogInformation("Provisioning candidate env {Namespace} for run {RunId} (image {Image})", ns, runId, image);

        await EnsureNamespaceAsync(ns, runId, ct);
        await client.AppsV1.CreateNamespacedDeploymentAsync(
            CandidateManifests.Deployment(ns, image, opts.CandidatePort, runId), ns, cancellationToken: ct);
        await client.CoreV1.CreateNamespacedServiceAsync(
            CandidateManifests.Service(ns, opts.CandidatePort, runId), ns, cancellationToken: ct);

        await WaitForDeploymentAsync(ns, TimeSpan.FromSeconds(opts.ReadyTimeoutSeconds), ct);

        return new EnvironmentHandle(CandidateManifests.CandidateUrl(ns, opts.CandidatePort), ns);
    }

    public async Task TeardownAsync(EnvironmentHandle handle, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(handle.Namespace))
        {
            return;
        }

        logger.LogInformation("Tearing down candidate namespace {Namespace}", handle.Namespace);
        try
        {
            await client.CoreV1.DeleteNamespaceAsync(handle.Namespace, cancellationToken: ct);
        }
        catch (HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Already gone; nothing to do.
        }
    }

    private async Task EnsureNamespaceAsync(string ns, string runId, CancellationToken ct)
    {
        try
        {
            await client.CoreV1.CreateNamespaceAsync(CandidateManifests.Namespace(ns, runId), cancellationToken: ct);
        }
        catch (HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            // A previous run for this PR left the namespace; reuse it.
        }
    }

    private async Task WaitForDeploymentAsync(string ns, TimeSpan timeout, CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            var deployment = await client.AppsV1.ReadNamespacedDeploymentAsync("candidate", ns, cancellationToken: ct);
            if (deployment.Status?.AvailableReplicas >= 1)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(3), ct);
        }

        throw new TimeoutException($"Candidate deployment in {ns} did not become available within {timeout}.");
    }
}
