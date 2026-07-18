using System.Net;
using System.Net.Http.Json;
using System.Text;
using ApiDiff.Api.Domain;
using ApiDiff.Api.Features.Capture;
using ApiDiff.Api.Features.Organizations;
using ApiDiff.Api.Features.Projects;
using ApiDiff.Api.Features.Scenarios;
using ApiDiff.Api.Persistence;
using ApiDiff.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ApiDiff.Api.Tests;

public class CaptureTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private HttpClient ClientFor(string subject)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, subject);
        return client;
    }

    private static string Slug(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..30];

    private async Task<(Guid orgId, Guid projectId)> CreateProjectAsync(HttpClient client)
    {
        var org = await (await client.PostAsJsonAsync("/api/v1/organizations",
            new CreateOrganizationRequest("Acme", Slug("acme")))).Content.ReadFromJsonAsync<OrganizationResponse>();
        var project = await (await client.PostAsJsonAsync($"/api/v1/organizations/{org!.Id}/projects",
            new CreateProjectRequest("Checkout", Slug("checkout"), "acme/checkout", "https://baseline.test")))
            .Content.ReadFromJsonAsync<ProjectResponse>();
        return (org.Id, project!.Id);
    }

    [Fact]
    public async Task Capture_SanitizesTraffic_NoSecretsReachStorage()
    {
        var client = ClientFor("owner-" + Guid.NewGuid());
        var (orgId, projectId) = await CreateProjectAsync(client);

        var request = new CaptureScenarioRequest(
            Method: "post",
            Path: "/v1/checkout",
            Query: "api_key=AKIAIOSFODNN7EXAMPLE&page=1",
            RequestHeaders: new() { ["Authorization"] = "Bearer topsecret", ["Content-Type"] = "application/json" },
            RequestContentType: "application/json",
            RequestBody: """{"password":"hunter2super","email":"alice@secret.example.com","card":"4111 1111 1111 1111","item":"book"}""",
            ReferenceStatusCode: 200,
            ReferenceHeaders: new() { ["Set-Cookie"] = "sid=1", ["Content-Type"] = "application/json" },
            ReferenceContentType: "application/json",
            ReferenceBody: """{"ssn":"123-45-6789","token":"eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NTYifQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c","status":"ok"}""");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/organizations/{orgId}/projects/{projectId}/scenarios", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Inspect what actually landed in the database.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiDiffDbContext>();
        var scenario = await db.Scenarios.SingleAsync(s => s.ProjectId == projectId);

        var stored = string.Join('\n',
            scenario.Query,
            scenario.RequestHeadersJson,
            Encoding.UTF8.GetString(scenario.RequestBody),
            scenario.ReferenceHeadersJson,
            Encoding.UTF8.GetString(scenario.ReferenceBody));

        foreach (var secret in SanitizerTests.Secrets)
        {
            Assert.DoesNotContain(secret, stored);
        }
        Assert.DoesNotContain("hunter2super", stored);
        Assert.DoesNotContain("topsecret", stored);
        Assert.DoesNotContain("Authorization", stored);

        // Benign data survives and the scenario is well-formed.
        Assert.Contains("book", stored);
        Assert.Contains("ok", stored);
        Assert.Equal("POST", scenario.Method);
        Assert.Equal(64, scenario.Fingerprint.Length);

        Assert.True(await db.AuditLogs.AnyAsync(a => a.Action == "scenario.captured" && a.TargetId == scenario.Id.ToString()));
    }

    [Fact]
    public async Task Capture_ThenListViaRest_ReturnsScenario()
    {
        var client = ClientFor("owner-" + Guid.NewGuid());
        var (orgId, projectId) = await CreateProjectAsync(client);

        var request = new CaptureScenarioRequest(
            "GET", "/v1/items/1", null, null, null, null, 200, null, null, """{"ok":true}""");
        var created = await client.PostAsJsonAsync(
            $"/api/v1/organizations/{orgId}/projects/{projectId}/scenarios", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var scenarios = await client.GetFromJsonAsync<List<ScenarioResponse>>(
            $"/api/v1/organizations/{orgId}/projects/{projectId}/scenarios");
        Assert.Single(scenarios!);
        Assert.Equal("/v1/items/1", scenarios![0].Path);
    }

    [Fact]
    public async Task Capture_NonMember_Forbidden()
    {
        var owner = ClientFor("owner-" + Guid.NewGuid());
        var (orgId, projectId) = await CreateProjectAsync(owner);

        var outsider = ClientFor("outsider-" + Guid.NewGuid());
        var request = new CaptureScenarioRequest("GET", "/v1/x", null, null, null, null, 200, null, null, null);
        var response = await outsider.PostAsJsonAsync(
            $"/api/v1/organizations/{orgId}/projects/{projectId}/scenarios", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
