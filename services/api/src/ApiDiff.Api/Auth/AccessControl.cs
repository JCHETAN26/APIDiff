using ApiDiff.Api.Domain;
using ApiDiff.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ApiDiff.Api.Auth;

/// <summary>Role-based access checks scoped to an organization.</summary>
public interface IAccessControl
{
    /// <summary>The caller's role in the organization, or null if not a member.</summary>
    Task<MembershipRole?> GetRoleAsync(Guid userId, Guid organizationId, CancellationToken ct = default);

    /// <summary>True when the caller holds at least <paramref name="minimum"/> in the organization.</summary>
    Task<bool> HasRoleAsync(Guid userId, Guid organizationId, MembershipRole minimum, CancellationToken ct = default);
}

public sealed class AccessControl(ApiDiffDbContext db) : IAccessControl
{
    public async Task<MembershipRole?> GetRoleAsync(Guid userId, Guid organizationId, CancellationToken ct = default)
    {
        var membership = await db.Memberships
            .SingleOrDefaultAsync(m => m.UserId == userId && m.OrganizationId == organizationId, ct);
        return membership?.Role;
    }

    public async Task<bool> HasRoleAsync(Guid userId, Guid organizationId, MembershipRole minimum, CancellationToken ct = default)
    {
        var role = await GetRoleAsync(userId, organizationId, ct);
        return role is not null && role >= minimum;
    }
}
