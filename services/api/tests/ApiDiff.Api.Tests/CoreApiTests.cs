using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ApiDiff.Api.Domain;
using ApiDiff.Api.Features.Organizations;
using ApiDiff.Api.Features.Projects;
using ApiDiff.Api.Features.Scenarios;
using ApiDiff.Api.Persistence;
using ApiDiff.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ApiDiff.Api.Tests;

public class CoreApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private HttpClient ClientFor(string? subject)
    {
        var client = factory.CreateClient();
        if (subject is not null)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, subject);
        }

        return client;
    }

    private static string Slug(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..30];

    [Fact]
    public async Task Unauthenticated_Request_Returns401()
    {
        var response = await ClientFor(null).GetAsync("/api/v1/organizations");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_Organization_Then_List_And_Audit()
    {
        var client = ClientFor("owner-" + Guid.NewGuid());
        var slug = Slug("acme");

        var create = await client.PostAsJsonAsync("/api/v1/organizations",
            new CreateOrganizationRequest("Acme", slug));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var org = await create.Content.ReadFromJsonAsync<OrganizationResponse>();
        Assert.NotNull(org);
        Assert.Equal(MembershipRole.Owner, org!.Role);

        var list = await client.GetFromJsonAsync<List<OrganizationResponse>>("/api/v1/organizations");
        Assert.Contains(list!, o => o.Id == org.Id);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiDiffDbContext>();
        Assert.True(await db.AuditLogs.AnyAsync(a => a.OrganizationId == org.Id && a.Action == "organization.created"));
    }

    [Fact]
    public async Task Owner_Creates_Project_And_Lists_Scenarios()
    {
        var client = ClientFor("owner-" + Guid.NewGuid());

        var orgResp = await (await client.PostAsJsonAsync("/api/v1/organizations",
            new CreateOrganizationRequest("Acme", Slug("acme")))).Content.ReadFromJsonAsync<OrganizationResponse>();
        var orgId = orgResp!.Id;

        var createProject = await client.PostAsJsonAsync($"/api/v1/organizations/{orgId}/projects",
            new CreateProjectRequest("Orders", Slug("orders"), "acme/orders", "https://baseline.test"));
        Assert.Equal(HttpStatusCode.Created, createProject.StatusCode);
        var project = await createProject.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.NotNull(project);

        // Seed a scenario (capture pipeline lands in Phase 3).
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApiDiffDbContext>();
            db.Scenarios.Add(new Scenario
            {
                Id = Guid.NewGuid(),
                ProjectId = project!.Id,
                Method = "GET",
                Path = "/v1/orders/1",
                Fingerprint = "fp-1",
                ReferenceStatusCode = 200,
                CapturedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
            Assert.True(await db.AuditLogs.AnyAsync(a => a.Action == "project.created" && a.TargetId == project.Id.ToString()));
        }

        var scenarios = await client.GetFromJsonAsync<List<ScenarioResponse>>(
            $"/api/v1/organizations/{orgId}/projects/{project!.Id}/scenarios");
        Assert.Single(scenarios!);
        Assert.Equal("/v1/orders/1", scenarios![0].Path);
    }

    [Fact]
    public async Task NonMember_Cannot_Create_Or_List_Projects()
    {
        var owner = ClientFor("owner-" + Guid.NewGuid());
        var orgResp = await (await owner.PostAsJsonAsync("/api/v1/organizations",
            new CreateOrganizationRequest("Acme", Slug("acme")))).Content.ReadFromJsonAsync<OrganizationResponse>();
        var orgId = orgResp!.Id;

        var outsider = ClientFor("outsider-" + Guid.NewGuid());

        var create = await outsider.PostAsJsonAsync($"/api/v1/organizations/{orgId}/projects",
            new CreateProjectRequest("Sneaky", Slug("sneaky"), "acme/x", "https://x.test"));
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);

        var list = await outsider.GetAsync($"/api/v1/organizations/{orgId}/projects");
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
    }

    [Fact]
    public async Task Invalid_Slug_Returns400()
    {
        var client = ClientFor("owner-" + Guid.NewGuid());
        var response = await client.PostAsJsonAsync("/api/v1/organizations",
            new CreateOrganizationRequest("Acme", "Not A Slug"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OpenApi_Document_Is_Published()
    {
        var response = await factory.CreateClient().GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("application/json", response.Content.Headers.ContentType?.MediaType);
    }
}
