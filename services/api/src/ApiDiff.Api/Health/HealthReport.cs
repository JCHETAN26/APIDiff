namespace ApiDiff.Api.Health;

/// <summary>Service health state reported over REST and (later) gRPC.</summary>
public sealed record HealthStatus(string Service, string Status, string Version);

/// <summary>Produces the current health status for the API service.</summary>
public static class HealthReport
{
    /// <summary>Service version; overridden by the build pipeline in later phases.</summary>
    public const string Version = "0.0.0";

    /// <summary>Returns the current health status.</summary>
    public static HealthStatus Current() => new("api", "ok", Version);
}
