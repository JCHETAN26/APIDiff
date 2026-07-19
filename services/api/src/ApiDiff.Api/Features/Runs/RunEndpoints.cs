using ApiDiff.Api.Auth;
using ApiDiff.Api.Domain;
using ApiDiff.Api.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiDiff.Api.Features.Runs;

public sealed record RunResponse(
    Guid Id, Guid ProjectId, int PullRequestNumber, string CommitSha, string Status,
    string? CandidateBaseUrl, DateTimeOffset CreatedAt, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt);

public sealed record RunDetailResponse(RunResponse Run, int TotalResults, int Regressions);

/// <summary>Read-only access to regression runs (the report surface).</summary>
public static class RunEndpoints
{
    public static RouteGroupBuilder MapRunEndpoints(this RouteGroupBuilder group)
    {
        var runs = group
            .MapGroup("/organizations/{organizationId:guid}/projects/{projectId:guid}/runs")
            .WithTags("Runs");

        runs.MapGet("/", async Task<Results<Ok<List<RunResponse>>, ForbidHttpResult>> (
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

            var entities = await db.RegressionRuns
                .Where(r => r.ProjectId == projectId && r.Project.OrganizationId == organizationId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(ct);
            return TypedResults.Ok(entities.Select(ToResponse).ToList());
        });

        runs.MapGet("/{runId:guid}", async Task<Results<Ok<RunDetailResponse>, ForbidHttpResult, NotFound>> (
            Guid organizationId,
            Guid projectId,
            Guid runId,
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

            var run = await db.RegressionRuns
                .SingleOrDefaultAsync(r => r.Id == runId && r.ProjectId == projectId && r.Project.OrganizationId == organizationId, ct);
            if (run is null)
            {
                return TypedResults.NotFound();
            }

            var total = await db.ReplayResults.CountAsync(r => r.RunId == runId, ct);
            var regressions = await db.ReplayResults.CountAsync(
                r => r.RunId == runId &&
                     (r.Verdict == RunVerdict.BehavioralRegression ||
                      r.Verdict == RunVerdict.PerfRegression ||
                      r.Verdict == RunVerdict.Error), ct);

            return TypedResults.Ok(new RunDetailResponse(ToResponse(run), total, regressions));
        });

        return group;
    }

    private static RunResponse ToResponse(RegressionRun r) =>
        new(r.Id, r.ProjectId, r.PullRequestNumber, r.CommitSha, r.Status.ToString(),
            r.CandidateBaseUrl, r.CreatedAt, r.StartedAt, r.CompletedAt);
}
