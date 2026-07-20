using ApiDiff.Api.Orchestration.Kubernetes;
using Xunit;

namespace ApiDiff.Api.Tests;

public class CandidateManifestsTests
{
    [Fact]
    public void NamespaceName_SubstitutesPr()
    {
        Assert.Equal("apidiff-pr-42", CandidateManifests.NamespaceName("apidiff-pr-{pr}", 42));
    }

    [Fact]
    public void ImageReference_SubstitutesPrAndSha()
    {
        var image = CandidateManifests.ImageReference("reg/candidate-{pr}:{sha}", 7, "abc123");
        Assert.Equal("reg/candidate-7:abc123", image);
    }

    [Fact]
    public void CandidateUrl_IsInClusterDns()
    {
        Assert.Equal("http://candidate.apidiff-pr-7.svc.cluster.local:8080",
            CandidateManifests.CandidateUrl("apidiff-pr-7", 8080));
    }

    [Fact]
    public void Deployment_HasRunLabelAndMatchingSelector()
    {
        var runId = Guid.NewGuid().ToString();
        var deployment = CandidateManifests.Deployment("apidiff-pr-7", "img:sha", 8080, runId);

        Assert.Equal("candidate", deployment.Metadata.Name);
        Assert.Equal("apidiff-pr-7", deployment.Metadata.NamespaceProperty);
        Assert.Equal(runId, deployment.Metadata.Labels[CandidateManifests.RunLabel]);

        // The selector must match the pod template labels or the deployment is invalid.
        var selector = deployment.Spec.Selector.MatchLabels[CandidateManifests.AppLabel];
        var podLabel = deployment.Spec.Template.Metadata.Labels[CandidateManifests.AppLabel];
        Assert.Equal(selector, podLabel);

        var container = Assert.Single(deployment.Spec.Template.Spec.Containers);
        Assert.Equal("img:sha", container.Image);
        Assert.Equal(8080, container.Ports[0].ContainerPort);
    }

    [Fact]
    public void Service_TargetsCandidatePods()
    {
        var service = CandidateManifests.Service("apidiff-pr-7", 8080, "run-1");
        Assert.Equal("candidate", service.Spec.Selector[CandidateManifests.AppLabel]);
        Assert.Equal(8080, service.Spec.Ports[0].Port);
    }

    [Fact]
    public void Namespace_CarriesRunLabel()
    {
        var ns = CandidateManifests.Namespace("apidiff-pr-7", "run-9");
        Assert.Equal("apidiff-pr-7", ns.Metadata.Name);
        Assert.Equal("run-9", ns.Metadata.Labels[CandidateManifests.RunLabel]);
        Assert.Equal("apidiff", ns.Metadata.Labels[CandidateManifests.ManagedByLabel]);
    }
}
