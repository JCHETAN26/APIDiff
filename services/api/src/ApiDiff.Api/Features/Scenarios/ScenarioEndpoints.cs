using ApiDiff.Api.Auth;
using ApiDiff.Api.Domain;
using ApiDiff.Api.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiDiff.Api.Features.Scenarios;

public sealed record ScenarioResponse(
    Guid Id, Guid ProjectId, string Method, string Path, string Query, int ReferenceStatusCode, string Fingerprint, DateTimeOffset CapturedAt);

/// <summary>
/// Read-only scenario access. Scenarios are written by the capture pipeline
/// (Phase 3); Phase 2 exposes listing and retrieval for the dashboard.
/// </summary>
public static class ScenarioEndpoints
{
    public static RouteGroupBuilder MapScenarioEndpoints(this RouteGroupBuilder group)
    {
        var scenarios = group
            .MapGroup("/organizations/{organizationId:guid}/projects/{projectId:guid}/scenarios")
            .WithTags("Scenarios");

        scenarios.MapGet("/", async Task<Results<Ok<List<ScenarioResponse>>, ForbidHttpResult, NotFound>> (
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

            if (!await db.Projects.AnyAsync(p => p.Id == projectId && p.OrganizationId == organizationId, ct))
            {
                return TypedResults.NotFound();
            }

            var result = await db.Scenarios
                .Where(s => s.ProjectId == projectId)
                .OrderByDescending(s => s.CapturedAt)
                .Select(s => new ScenarioResponse(
                    s.Id, s.ProjectId, s.Method, s.Path, s.Query, s.ReferenceStatusCode, s.Fingerprint, s.CapturedAt))
                .ToListAsync(ct);
            return TypedResults.Ok(result);
        });

        scenarios.MapGet("/{scenarioId:guid}", async Task<Results<Ok<ScenarioResponse>, ForbidHttpResult, NotFound>> (
            Guid organizationId,
            Guid projectId,
            Guid scenarioId,
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

            var scenario = await db.Scenarios
                .Where(s => s.Id == scenarioId && s.ProjectId == projectId && s.Project.OrganizationId == organizationId)
                .Select(s => new ScenarioResponse(
                    s.Id, s.ProjectId, s.Method, s.Path, s.Query, s.ReferenceStatusCode, s.Fingerprint, s.CapturedAt))
                .SingleOrDefaultAsync(ct);
            return scenario is null ? TypedResults.NotFound() : TypedResults.Ok(scenario);
        });

        return group;
    }
}
