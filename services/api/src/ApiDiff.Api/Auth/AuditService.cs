using System.Text.Json;
using ApiDiff.Api.Domain;
using ApiDiff.Api.Persistence;

namespace ApiDiff.Api.Auth;

/// <summary>
/// Appends audit entries to the current unit of work. Entries are persisted by
/// the caller's <c>SaveChangesAsync</c>, so they commit atomically with the
/// mutation they describe (ADR 0003).
/// </summary>
public interface IAuditService
{
    void Append(Guid organizationId, Guid? actorUserId, string action, string targetType, string targetId, object? metadata = null);
}

public sealed class AuditService(ApiDiffDbContext db) : IAuditService
{
    public void Append(Guid organizationId, Guid? actorUserId, string action, string targetType, string targetId, object? metadata = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            OrganizationId = organizationId,
            ActorUserId = actorUserId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            MetadataJson = metadata is null ? "{}" : JsonSerializer.Serialize(metadata),
            CreatedAt = DateTimeOffset.UtcNow,
        });
    }
}
