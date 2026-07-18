namespace ApiDiff.Api.Domain;

/// <summary>A service under test: links a GitHub repo, a baseline env, and capture config.</summary>
public class Project
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public string Name { get; set; } = null!;

    /// <summary>URL-safe identifier, unique within the organization.</summary>
    public string Slug { get; set; } = null!;

    /// <summary>owner/repo on GitHub.</summary>
    public string GitHubRepo { get; set; } = null!;

    /// <summary>Base URL of the current baseline environment.</summary>
    public string BaselineBaseUrl { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Scenario> Scenarios { get; set; } = new List<Scenario>();
    public ICollection<ScenarioCluster> Clusters { get; set; } = new List<ScenarioCluster>();
    public ICollection<RegressionRun> Runs { get; set; } = new List<RegressionRun>();
}

/// <summary>One sanitized captured request plus its reference (baseline) response.</summary>
public class Scenario
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public string Method { get; set; } = null!;
    public string Path { get; set; } = null!;
    public string Query { get; set; } = "";

    /// <summary>Sanitized request headers as JSON.</summary>
    public string RequestHeadersJson { get; set; } = "{}";

    /// <summary>Sanitized request body.</summary>
    public byte[] RequestBody { get; set; } = [];

    public int ReferenceStatusCode { get; set; }
    public string ReferenceHeadersJson { get; set; } = "{}";
    public byte[] ReferenceBody { get; set; } = [];

    /// <summary>Stable content hash used to detect and cluster duplicates.</summary>
    public string Fingerprint { get; set; } = null!;

    public DateTimeOffset CapturedAt { get; set; }

    public Guid? ClusterId { get; set; }
    public ScenarioCluster? Cluster { get; set; }
}

/// <summary>A group of duplicate/near-duplicate scenarios represented by one member.</summary>
public class ScenarioCluster
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public Guid RepresentativeScenarioId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Scenario> Members { get; set; } = new List<Scenario>();
}
