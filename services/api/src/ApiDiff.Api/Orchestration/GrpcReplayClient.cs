using System.Text.Json;
using ApiDiff.Api.Domain;
using Google.Protobuf;
using Grpc.Core;
using CommonV1 = ApiDiff.Contracts.Common.V1;
using ReplayV1 = ApiDiff.Contracts.Replay.V1;

namespace ApiDiff.Api.Orchestration;

/// <summary>Calls the Go replay engine over gRPC and maps results to the domain.</summary>
public sealed class GrpcReplayClient(ReplayV1.ReplayService.ReplayServiceClient client) : IReplayClient
{
    public async Task<IReadOnlyList<ReplayOutcome>> ReplayAsync(
        RegressionRun run,
        IReadOnlyList<Scenario> scenarios,
        string baselineUrl,
        string candidateUrl,
        CancellationToken ct)
    {
        var request = new ReplayV1.ReplayRequest
        {
            RunId = run.Id.ToString(),
            Baseline = new CommonV1.Target { Label = "baseline", BaseUrl = baselineUrl },
            Candidate = new CommonV1.Target { Label = "candidate", BaseUrl = candidateUrl },
        };
        foreach (var scenario in scenarios)
        {
            request.Scenarios.Add(ToProto(scenario));
        }

        var outcomes = new List<ReplayOutcome>(scenarios.Count);
        using var call = client.Replay(request, cancellationToken: ct);
        await foreach (var message in call.ResponseStream.ReadAllAsync(ct))
        {
            outcomes.Add(ToOutcome(message.Result));
        }

        return outcomes;
    }

    private static ReplayV1.Scenario ToProto(Scenario s)
    {
        var proto = new ReplayV1.Scenario
        {
            Id = s.Id.ToString(),
            Request = new CommonV1.HttpRequest
            {
                Method = s.Method,
                Path = s.Path,
                Query = s.Query,
                Body = ByteString.CopyFrom(s.RequestBody),
            },
            ReferenceResponse = new CommonV1.HttpResponse
            {
                StatusCode = s.ReferenceStatusCode,
                Body = ByteString.CopyFrom(s.ReferenceBody),
            },
        };
        AddHeaders(proto.Request.Headers, s.RequestHeadersJson);
        AddHeaders(proto.ReferenceResponse.Headers, s.ReferenceHeadersJson);
        return proto;
    }

    private static void AddHeaders(ICollection<CommonV1.Header> target, string headersJson)
    {
        var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson) ?? [];
        foreach (var (name, value) in headers)
        {
            target.Add(new CommonV1.Header { Name = name, Value = value });
        }
    }

    private static ReplayOutcome ToOutcome(ReplayV1.ReplayResult result)
    {
        Guid.TryParse(result.ScenarioId, out var scenarioId);
        return new ReplayOutcome(
            ScenarioId: scenarioId,
            Verdict: (RunVerdict)(int)result.Verdict,
            DiffJson: result.Diff is null ? "{}" : JsonFormatter.Default.Format(result.Diff),
            BaselineLatencyMs: result.BaselineResponse?.LatencyMs ?? 0,
            CandidateLatencyMs: result.CandidateResponse?.LatencyMs ?? 0,
            LatencyDeltaMs: result.LatencyDeltaMs,
            Error: string.IsNullOrEmpty(result.Error) ? null : result.Error);
    }
}
