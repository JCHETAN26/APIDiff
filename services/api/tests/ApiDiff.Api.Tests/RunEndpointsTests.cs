using System.Net.Http.Json;
using ApiDiff.Api.Domain;
using ApiDiff.Api.Features.Organizations;
using ApiDiff.Api.Features.Projects;
using ApiDiff.Api.Features.Runs;
using ApiDiff.Api.Persistence;
using ApiDiff.Api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ApiDiff.Api.Tests;

public class RunEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private HttpClient ClientFor(string subject)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, subject);
        return client;
    }

    private static string Slug(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..30];

    [Fact]
    public async Task Run_List_Detail_And_Results()
    {
        var client = ClientFor("owner-" + Guid.NewGuid());

        var org = await (await client.PostAsJsonAsync("/api/v1/organizations",
            new CreateOrganizationRequest("Acme", Slug("acme")))).Content.ReadFromJsonAsync<OrganizationResponse>();
        var project = await (await client.PostAsJsonAsync($"/api/v1/organizations/{org!.Id}/projects",
            new CreateProjectRequest("Orders", Slug("orders"), "acme/orders", "https://baseline.test")))
            .Content.ReadFromJsonAsync<ProjectResponse>();

        var runId = Guid.NewGuid();
        var s1 = Guid.NewGuid();
        var s2 = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApiDiffDbContext>();
            db.Scenarios.Add(new Scenario { Id = s1, ProjectId = project!.Id, Method = "GET", Path = "/a", Fingerprint = "f1", ReferenceStatusCode = 200, CapturedAt = DateTimeOffset.UtcNow });
            db.Scenarios.Add(new Scenario { Id = s2, ProjectId = project.Id, Method = "GET", Path = "/b", Fingerprint = "f2", ReferenceStatusCode = 200, CapturedAt = DateTimeOffset.UtcNow });
            db.RegressionRuns.Add(new RegressionRun { Id = runId, ProjectId = project.Id, PullRequestNumber = 3, CommitSha = "abc", Status = RunStatus.Completed, CreatedAt = DateTimeOffset.UtcNow });
            db.ReplayResults.Add(new ReplayResult { Id = Guid.NewGuid(), RunId = runId, ScenarioId = s1, Verdict = RunVerdict.Pass, DiffJson = "{}", BaselineLatencyMs = 10, CandidateLatencyMs = 11, LatencyDeltaMs = 1, CreatedAt = DateTimeOffset.UtcNow });
            db.ReplayResults.Add(new ReplayResult { Id = Guid.NewGuid(), RunId = runId, ScenarioId = s2, Verdict = RunVerdict.BehavioralRegression, DiffJson = """{"fields":[{"path":"total"}]}""", BaselineLatencyMs = 10, CandidateLatencyMs = 40, LatencyDeltaMs = 30, CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }

        var runs = await client.GetFromJsonAsync<List<RunResponse>>($"/api/v1/organizations/{org.Id}/projects/{project!.Id}/runs");
        Assert.Contains(runs!, r => r.Id == runId && r.Status == "Completed");

        var detail = await client.GetFromJsonAsync<RunDetailResponse>($"/api/v1/organizations/{org.Id}/projects/{project.Id}/runs/{runId}");
        Assert.Equal(2, detail!.TotalResults);
        Assert.Equal(1, detail.Regressions);

        var results = await client.GetFromJsonAsync<List<ReplayResultResponse>>($"/api/v1/organizations/{org.Id}/projects/{project.Id}/runs/{runId}/results");
        Assert.Equal(2, results!.Count);
        // Ordered by latency delta descending: the regression (30ms) comes first.
        Assert.Equal("BehavioralRegression", results[0].Verdict);
        Assert.Equal(30, results[0].LatencyDeltaMs);
        Assert.Contains("total", results[0].DiffJson);
    }
}
