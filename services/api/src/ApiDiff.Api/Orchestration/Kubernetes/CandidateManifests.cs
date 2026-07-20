using k8s.Models;

namespace ApiDiff.Api.Orchestration.Kubernetes;

/// <summary>
/// Pure builders for the Kubernetes objects that make up an ephemeral per-PR
/// candidate environment. Kept free of any client calls so they are unit-testable
/// without a cluster.
/// </summary>
public static class CandidateManifests
{
    public const string AppLabel = "app.kubernetes.io/name";
    public const string PartOfLabel = "app.kubernetes.io/part-of";
    public const string ManagedByLabel = "app.kubernetes.io/managed-by";
    public const string RunLabel = "apidiff.dev/run-id";

    /// <summary>Resolves the namespace name for a PR.</summary>
    public static string NamespaceName(string template, int pullRequestNumber) =>
        template.Replace("{pr}", pullRequestNumber.ToString(), StringComparison.Ordinal);

    /// <summary>Resolves the candidate image reference.</summary>
    public static string ImageReference(string template, int pullRequestNumber, string commitSha) =>
        template
            .Replace("{pr}", pullRequestNumber.ToString(), StringComparison.Ordinal)
            .Replace("{sha}", commitSha, StringComparison.Ordinal);

    /// <summary>In-cluster URL of the candidate service.</summary>
    public static string CandidateUrl(string ns, int port) =>
        $"http://candidate.{ns}.svc.cluster.local:{port}";

    private static IDictionary<string, string> Labels(string runId) => new Dictionary<string, string>
    {
        [AppLabel] = "candidate",
        [PartOfLabel] = "apidiff",
        [ManagedByLabel] = "apidiff",
        [RunLabel] = runId,
    };

    public static V1Namespace Namespace(string ns, string runId) => new()
    {
        Metadata = new V1ObjectMeta { Name = ns, Labels = Labels(runId) },
    };

    public static V1Deployment Deployment(string ns, string image, int port, string runId)
    {
        var labels = Labels(runId);
        return new V1Deployment
        {
            Metadata = new V1ObjectMeta { Name = "candidate", NamespaceProperty = ns, Labels = labels },
            Spec = new V1DeploymentSpec
            {
                Replicas = 1,
                Selector = new V1LabelSelector { MatchLabels = new Dictionary<string, string> { [AppLabel] = "candidate" } },
                Template = new V1PodTemplateSpec
                {
                    Metadata = new V1ObjectMeta { Labels = labels },
                    Spec = new V1PodSpec
                    {
                        Containers =
                        [
                            new V1Container
                            {
                                Name = "candidate",
                                Image = image,
                                Ports = [new V1ContainerPort { ContainerPort = port }],
                            },
                        ],
                    },
                },
            },
        };
    }

    public static V1Service Service(string ns, int port, string runId) => new()
    {
        Metadata = new V1ObjectMeta { Name = "candidate", NamespaceProperty = ns, Labels = Labels(runId) },
        Spec = new V1ServiceSpec
        {
            Selector = new Dictionary<string, string> { [AppLabel] = "candidate" },
            Ports = [new V1ServicePort { Port = port, TargetPort = port }],
        },
    };
}
