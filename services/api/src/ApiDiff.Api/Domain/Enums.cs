namespace ApiDiff.Api.Domain;

/// <summary>A member's role within an organization.</summary>
public enum MembershipRole
{
    Viewer = 0,
    Member = 1,
    Admin = 2,
    Owner = 3,
}

/// <summary>Lifecycle state of a regression run.</summary>
public enum RunStatus
{
    Pending = 0,
    Provisioning = 1,
    Replaying = 2,
    Analyzing = 3,
    Completed = 4,
    Failed = 5,
    Cancelled = 6,
}

/// <summary>Per-scenario replay verdict. Mirrors apidiff.common.v1.Verdict.</summary>
public enum RunVerdict
{
    Unspecified = 0,
    Pass = 1,
    BehavioralRegression = 2,
    PerfRegression = 3,
    Error = 4,
}
