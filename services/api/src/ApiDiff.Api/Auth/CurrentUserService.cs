using System.Security.Claims;
using ApiDiff.Api.Domain;
using ApiDiff.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ApiDiff.Api.Auth;

/// <summary>Resolves (and just-in-time provisions) the authenticated user.</summary>
public interface ICurrentUserService
{
    /// <summary>The subject claim of the caller, or null if unauthenticated.</summary>
    string? Subject { get; }

    /// <summary>
    /// Returns the <see cref="User"/> for the caller, creating it on first sight.
    /// Throws <see cref="UnauthorizedAccessException"/> if unauthenticated.
    /// </summary>
    Task<User> GetOrProvisionAsync(CancellationToken ct = default);
}

public sealed class CurrentUserService(
    IHttpContextAccessor httpContextAccessor,
    ApiDiffDbContext db,
    IAuditService audit) : ICurrentUserService
{
    private User? _cached;

    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public string? Subject =>
        Principal?.FindFirstValue("sub") ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

    public async Task<User> GetOrProvisionAsync(CancellationToken ct = default)
    {
        if (_cached is not null)
        {
            return _cached;
        }

        var subject = Subject
            ?? throw new UnauthorizedAccessException("No authenticated subject.");

        var existing = await db.Users.FirstOrDefaultAsync(u => u.ExternalSubject == subject, ct);
        if (existing is not null)
        {
            return _cached = existing;
        }

        var email = Principal?.FindFirstValue("email") ?? $"{subject}@unknown.local";
        var name = Principal?.FindFirstValue("name") ?? email;

        var user = new User
        {
            Id = Guid.NewGuid(),
            ExternalSubject = subject,
            Email = email,
            DisplayName = name,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.Users.Add(user);
        audit.Append(Guid.Empty, user.Id, "user.provisioned", nameof(User), user.Id.ToString());

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Lost a race with a concurrent first request; reload the winner.
            db.ChangeTracker.Clear();
            user = await db.Users.FirstAsync(u => u.ExternalSubject == subject, ct);
        }

        return _cached = user;
    }
}
