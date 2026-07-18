namespace ApiDiff.Api.Domain;

/// <summary>Append-only record of a mutating action, for compliance and forensics.</summary>
public class AuditLog
{
    public long Id { get; set; }

    public Guid OrganizationId { get; set; }

    /// <summary>Actor who performed the action; null for system/automated actions.</summary>
    public Guid? ActorUserId { get; set; }

    /// <summary>Dotted action name, e.g. "project.created", "run.started".</summary>
    public string Action { get; set; } = null!;

    /// <summary>Type of the affected entity, e.g. "Project".</summary>
    public string TargetType { get; set; } = null!;

    /// <summary>Identifier of the affected entity.</summary>
    public string TargetId { get; set; } = null!;

    /// <summary>Additional context as JSON.</summary>
    public string MetadataJson { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; }
}
