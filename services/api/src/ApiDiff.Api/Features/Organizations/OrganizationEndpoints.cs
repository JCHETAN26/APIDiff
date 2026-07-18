using ApiDiff.Api.Auth;
using ApiDiff.Api.Common;
using ApiDiff.Api.Domain;
using ApiDiff.Api.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiDiff.Api.Features.Organizations;

public sealed record CreateOrganizationRequest(string Name, string Slug);

public sealed record OrganizationResponse(Guid Id, string Name, string Slug, MembershipRole Role, DateTimeOffset CreatedAt);

public static class OrganizationEndpoints
{
    public static RouteGroupBuilder MapOrganizationEndpoints(this RouteGroupBuilder group)
    {
        var orgs = group.MapGroup("/organizations").WithTags("Organizations");

        // Create an organization; the caller becomes its Owner.
        orgs.MapPost("/", async (
            CreateOrganizationRequest request,
            ICurrentUserService currentUser,
            IAuditService audit,
            ApiDiffDbContext db,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name) || !Slug.IsValid(request.Slug))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["name"] = string.IsNullOrWhiteSpace(request.Name) ? ["Name is required."] : [],
                    ["slug"] = Slug.IsValid(request.Slug) ? [] : ["Slug must be lowercase alphanumeric with hyphens."],
                });
            }

            var user = await currentUser.GetOrProvisionAsync(ct);

            if (await db.Organizations.AnyAsync(o => o.Slug == request.Slug, ct))
            {
                return Results.Conflict(new { message = $"Organization slug '{request.Slug}' is taken." });
            }

            var now = DateTimeOffset.UtcNow;
            var org = new Organization { Id = Guid.NewGuid(), Name = request.Name, Slug = request.Slug, CreatedAt = now };
            db.Organizations.Add(org);
            db.Memberships.Add(new Membership
            {
                Id = Guid.NewGuid(),
                OrganizationId = org.Id,
                UserId = user.Id,
                Role = MembershipRole.Owner,
                CreatedAt = now,
            });
            audit.Append(org.Id, user.Id, "organization.created", nameof(Organization), org.Id.ToString(), new { org.Name, org.Slug });

            await db.SaveChangesAsync(ct);

            var response = new OrganizationResponse(org.Id, org.Name, org.Slug, MembershipRole.Owner, org.CreatedAt);
            return Results.Created($"/api/v1/organizations/{org.Id}", response);
        });

        // List organizations the caller belongs to.
        orgs.MapGet("/", async (
            ICurrentUserService currentUser,
            ApiDiffDbContext db,
            CancellationToken ct) =>
        {
            var user = await currentUser.GetOrProvisionAsync(ct);
            var result = await db.Memberships
                .Where(m => m.UserId == user.Id)
                .Select(m => new OrganizationResponse(
                    m.Organization.Id, m.Organization.Name, m.Organization.Slug, m.Role, m.Organization.CreatedAt))
                .ToListAsync(ct);
            return TypedResults.Ok(result);
        });

        // Get one organization the caller belongs to.
        orgs.MapGet("/{organizationId:guid}", async Task<Results<Ok<OrganizationResponse>, ForbidHttpResult, NotFound>> (
            Guid organizationId,
            ICurrentUserService currentUser,
            IAccessControl access,
            ApiDiffDbContext db,
            CancellationToken ct) =>
        {
            var user = await currentUser.GetOrProvisionAsync(ct);
            var role = await access.GetRoleAsync(user.Id, organizationId, ct);
            if (role is null)
            {
                return TypedResults.Forbid();
            }

            var org = await db.Organizations.FindAsync([organizationId], ct);
            return org is null
                ? TypedResults.NotFound()
                : TypedResults.Ok(new OrganizationResponse(org.Id, org.Name, org.Slug, role.Value, org.CreatedAt));
        });

        return group;
    }
}
