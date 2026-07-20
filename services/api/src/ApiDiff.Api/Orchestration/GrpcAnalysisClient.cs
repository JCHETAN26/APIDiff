using Google.Protobuf;
using AnalysisV1 = ApiDiff.Contracts.Analysis.V1;
using CommonV1 = ApiDiff.Contracts.Common.V1;
using ReplayV1 = ApiDiff.Contracts.Replay.V1;

namespace ApiDiff.Api.Orchestration;

/// <summary>Calls the Python analysis service over gRPC to explain failures.</summary>
public sealed class GrpcAnalysisClient(AnalysisV1.AnalysisService.AnalysisServiceClient client) : IAnalysisClient
{
    public async Task<IReadOnlyList<ExplanationDto>> ExplainAsync(
        string runId, IReadOnlyList<ReplayOutcome> failures, CancellationToken ct)
    {
        var request = new AnalysisV1.ExplainFailuresRequest { RunId = runId };
        foreach (var outcome in failures)
        {
            request.Failures.Add(ToProto(outcome));
        }

        var response = await client.ExplainFailuresAsync(request, cancellationToken: ct);

        return response.Explanations
            .Select(e => new ExplanationDto(e.Title, e.Detail, e.ScenarioIds.ToList(), e.Severity, e.LikelyCause))
            .ToList();
    }

    private static ReplayV1.ReplayResult ToProto(ReplayOutcome outcome)
    {
        return new ReplayV1.ReplayResult
        {
            ScenarioId = outcome.ScenarioId.ToString(),
            Verdict = (CommonV1.Verdict)(int)outcome.Verdict,
            LatencyDeltaMs = outcome.LatencyDeltaMs,
            Error = outcome.Error ?? "",
            Diff = ParseDiff(outcome.DiffJson),
        };
    }

    private static ReplayV1.Diff? ParseDiff(string diffJson)
    {
        if (string.IsNullOrWhiteSpace(diffJson))
        {
            return null;
        }

        try
        {
            return JsonParser.Default.Parse<ReplayV1.Diff>(diffJson);
        }
        catch (InvalidProtocolBufferException)
        {
            return null;
        }
    }
}
