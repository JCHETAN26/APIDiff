namespace ApiDiff.Api.Domain;

/// <summary>
/// A ranked, human-readable explanation of a group of related failures in a run,
/// produced by the analysis service.
/// </summary>
public class RunExplanation
{
    public Guid Id { get; set; }

    public Guid RunId { get; set; }
    public RegressionRun Run { get; set; } = null!;

    public string Title { get; set; } = null!;
    public string Detail { get; set; } = "";

    /// <summary>Ids of the scenarios this explanation covers, as a JSON array.</summary>
    public string ScenarioIdsJson { get; set; } = "[]";

    /// <summary>Ranking score in [0, 1]; higher is more severe.</summary>
    public double Severity { get; set; }

    public string LikelyCause { get; set; } = "";

    public DateTimeOffset CreatedAt { get; set; }
}
