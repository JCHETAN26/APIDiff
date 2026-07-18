using ApiDiff.Api.Auth;
using ApiDiff.Api.Common;
using ApiDiff.Api.Domain;
using ApiDiff.Api.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiDiff.Api.Features.Projects;

public sealed record CreateProjectRequest(string Name, string Slug, string GitHubRepo, string BaselineBaseUrl);

public sealed record ProjectResponse(
    Guid Id, Guid OrganizationId, string Name, string Slug, string GitHubRepo, string BaselineBaseUrl, DateTimeOffset CreatedAt);

public static class ProjectEndpoints
{
    public static RouteGroupBuilder MapProjectEndpoints(this RouteGroupBuilder group)
    {
        var projects = group.MapGroup("/organizations/{organizationId:guid}/projects").WithTags("Projects");

        // Create a project. Requires Admin or Owner in the organization.
        projects.MapPost("/", async Task<Results<Created<ProjectResponse>, ValidationProblem, ForbidHttpResult, Conflict<string>>> (
            Guid organizationId,
            CreateProjectRequest request,
            ICurrentUserService currentUser,
            IAccessControl access,
            IAuditService audit,
            ApiDiffDbContext db,
            CancellationToken ct) =>
        {
            var errors = new Dictionary<string, string[]>();
            if (string.IsNullOrWhiteSpace(request.Name)) errors["name"] = ["Name is required."];
            if (!Slug.IsValid(request.Slug)) errors["slug"] = ["Slug must be lowercase alphanumeric with hyphens."];
            if (string.IsNullOrWhiteSpace(request.GitHubRepo)) errors["gitHubRepo"] = ["GitHub repo is required."];
            if (string.IsNullOrWhiteSpace(request.BaselineBaseUrl)) errors["baselineBaseUrl"] = ["Baseline base URL is required."];
            if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

            var user = await currentUser.GetOrProvisionAsync(ct);
            if (!await access.HasRoleAsync(user.Id, organizationId, MembershipRole.Admin, ct))
            {
                return TypedResults.Forbid();
            }

            if (await db.Projects.AnyAsync(p => p.OrganizationId == organizationId && p.Slug == request.Slug, ct))
            {
                return TypedResults.Conflict($"Project slug '{request.Slug}' is taken in this organization.");
            }

            var project = new Project
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                Name = request.Name,
                Slug = request.Slug,
                GitHubRepo = request.GitHubRepo,
                BaselineBaseUrl = request.BaselineBaseUrl,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            db.Projects.Add(project);
            audit.Append(organizationId, user.Id, "project.created", nameof(Project), project.Id.ToString(), new { project.Name, project.Slug });

            await db.SaveChangesAsync(ct);

            return TypedResults.Created($"/api/v1/organizations/{organizationId}/projects/{project.Id}", ToResponse(project));
        });

        // List projects in the organization. Requires membership.
        projects.MapGet("/", async Task<Results<Ok<List<ProjectResponse>>, ForbidHttpResult>> (
            Guid organizationId,
            ICurrentUserService currentUser,
            IAccessControl access,
            ApiDiffDbContext db,
            CancellationToken ct) =>
        {
            var user = await currentUser.GetOrProvisionAsync(ct);
            if (!await access.HasRoleAsync(user.Id, organizationId, MembershipRole.Viewer, ct))
            {
                return TypedResults.Forbid();
            }

            var result = await db.Projects
                .Where(p => p.OrganizationId == organizationId)
                .OrderBy(p => p.Name)
                .Select(p => new ProjectResponse(
                    p.Id, p.OrganizationId, p.Name, p.Slug, p.GitHubRepo, p.BaselineBaseUrl, p.CreatedAt))
                .ToListAsync(ct);
            return TypedResults.Ok(result);
        });

        // Get a single project. Requires membership.
        projects.MapGet("/{projectId:guid}", async Task<Results<Ok<ProjectResponse>, ForbidHttpResult, NotFound>> (
            Guid organizationId,
            Guid projectId,
            ICurrentUserService currentUser,
            IAccessControl access,
            ApiDiffDbContext db,
            CancellationToken ct) =>
        {
            var user = await currentUser.GetOrProvisionAsync(ct);
            if (!await access.HasRoleAsync(user.Id, organizationId, MembershipRole.Viewer, ct))
            {
                return TypedResults.Forbid();
            }

            var project = await db.Projects
                .SingleOrDefaultAsync(p => p.Id == projectId && p.OrganizationId == organizationId, ct);
            return project is null ? TypedResults.NotFound() : TypedResults.Ok(ToResponse(project));
        });

        return group;
    }

    private static ProjectResponse ToResponse(Project p) =>
        new(p.Id, p.OrganizationId, p.Name, p.Slug, p.GitHubRepo, p.BaselineBaseUrl, p.CreatedAt);
}
