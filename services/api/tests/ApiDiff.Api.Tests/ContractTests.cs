using ApiDiff.Contracts.Common.V1;
using ApiDiff.Contracts.Replay.V1;
using Google.Protobuf;
using Xunit;

namespace ApiDiff.Api.Tests;

/// <summary>
/// Contract test: the C# gRPC stubs generated from proto/ compile and round-trip.
/// Satisfies the Phase 1 goal that generated stubs compile in each language.
/// </summary>
public class ContractTests
{
    [Fact]
    public void ReplayRequest_Roundtrips()
    {
        var request = new ReplayRequest { RunId = "run-1" };
        request.Scenarios.Add(new Scenario { Id = "s-1" });

        var parsed = ReplayRequest.Parser.ParseFrom(request.ToByteArray());

        Assert.Equal("run-1", parsed.RunId);
        Assert.Equal("s-1", parsed.Scenarios[0].Id);
    }

    [Fact]
    public void Verdict_EnumMatchesContract()
    {
        Assert.Equal(1, (int)Verdict.Pass);
        Assert.Equal(2, (int)Verdict.BehavioralRegression);
    }
}
