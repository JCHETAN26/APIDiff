namespace ApiDiff.Api.Domain;

/// <summary>A tenant that owns projects and members.</summary>
public class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;

    /// <summary>URL-safe unique identifier, e.g. "acme".</summary>
    public string Slug { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}

/// <summary>An authenticated principal (identified by an external OIDC subject).</summary>
public class User
{
    public Guid Id { get; set; }

    /// <summary>Stable subject claim from the identity provider.</summary>
    public string ExternalSubject { get; set; } = null!;

    public string Email { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
}

/// <summary>Join entity granting a <see cref="User"/> a role in an <see cref="Organization"/>.</summary>
public class Membership
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public MembershipRole Role { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
