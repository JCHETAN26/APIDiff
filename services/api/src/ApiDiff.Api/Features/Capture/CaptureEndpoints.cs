using ApiDiff.Api.Auth;
using ApiDiff.Api.Capture;
using ApiDiff.Api.Domain;
using ApiDiff.Api.Features.Scenarios;
using ApiDiff.Api.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiDiff.Api.Features.Capture;

/// <summary>A captured HTTP exchange submitted by the staging capture agent.</summary>
public sealed record CaptureScenarioRequest(
    string Method,
    string Path,
    string? Query,
    Dictionary<string, string>? RequestHeaders,
    string? RequestContentType,
    string? RequestBody,
    int ReferenceStatusCode,
    Dictionary<string, string>? ReferenceHeaders,
    string? ReferenceContentType,
    string? ReferenceBody);

/// <summary>
/// Capture ingest: accepts staging traffic, sanitizes it (PII/secret redaction,
/// header allowlist), and persists it as a <see cref="Scenario"/>. Requires
/// Member+ in the organization.
/// </summary>
public static class CaptureEndpoints
{
    public static RouteGroupBuilder MapCaptureEndpoints(this RouteGroupBuilder group)
    {
        var capture = group
            .MapGroup("/organizations/{organizationId:guid}/projects/{projectId:guid}/scenarios")
            .WithTags("Capture");

        capture.MapPost("/", async Task<Results<Created<ScenarioResponse>, ValidationProblem, ForbidHttpResult, NotFound>> (
            Guid organizationId,
            Guid projectId,
            CaptureScenarioRequest request,
            ICurrentUserService currentUser,
            IAccessControl access,
            ISanitizer sanitizer,
            IAuditService audit,
            ApiDiffDbContext db,
            CancellationToken ct) =>
        {
            var errors = new Dictionary<string, string[]>();
            if (string.IsNullOrWhiteSpace(request.Method)) errors["method"] = ["Method is required."];
            if (string.IsNullOrWhiteSpace(request.Path)) errors["path"] = ["Path is required."];
            if (request.ReferenceStatusCode is < 100 or > 599) errors["referenceStatusCode"] = ["Must be a valid HTTP status code."];
            if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

            var user = await currentUser.GetOrProvisionAsync(ct);
            if (!await access.HasRoleAsync(user.Id, organizationId, MembershipRole.Member, ct))
            {
                return TypedResults.Forbid();
            }

            if (!await db.Projects.AnyAsync(p => p.Id == projectId && p.OrganizationId == organizationId, ct))
            {
                return TypedResults.NotFound();
            }

            var query = sanitizer.SanitizeQuery(request.Query);
            var requestBody = sanitizer.SanitizeBody(request.RequestContentType, request.RequestBody);

            var scenario = new Scenario
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Method = request.Method.ToUpperInvariant(),
                Path = request.Path,
                Query = query,
                RequestHeadersJson = sanitizer.SanitizeHeadersToJson(request.RequestHeaders ?? new()),
                RequestBody = requestBody,
                ReferenceStatusCode = request.ReferenceStatusCode,
                ReferenceHeadersJson = sanitizer.SanitizeHeadersToJson(request.ReferenceHeaders ?? new()),
                ReferenceBody = sanitizer.SanitizeBody(request.ReferenceContentType, request.ReferenceBody),
                Fingerprint = Fingerprint.Compute(request.Method, request.Path, query, requestBody),
                CapturedAt = DateTimeOffset.UtcNow,
            };
            db.Scenarios.Add(scenario);
            audit.Append(organizationId, user.Id, "scenario.captured", nameof(Scenario), scenario.Id.ToString(),
                new { scenario.Method, scenario.Path });

            await db.SaveChangesAsync(ct);

            var response = new ScenarioResponse(
                scenario.Id, scenario.ProjectId, scenario.Method, scenario.Path, scenario.Query,
                scenario.ReferenceStatusCode, scenario.Fingerprint, scenario.CapturedAt);
            return TypedResults.Created(
                $"/api/v1/organizations/{organizationId}/projects/{projectId}/scenarios/{scenario.Id}", response);
        });

        return group;
    }
}
