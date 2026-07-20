from apidiff.common.v1 import common_pb2
from apidiff.replay.v1 import replay_pb2

from analysis.explain import explain_failures


def behavioral(scenario_id: str, path: str, kind: int = replay_pb2.DIFF_KIND_CHANGED,
               baseline: str = "10", candidate: str = "12") -> object:
    field = replay_pb2.FieldDiff(
        path=path, kind=kind, baseline_value=baseline, candidate_value=candidate
    )
    return replay_pb2.ReplayResult(
        scenario_id=scenario_id,
        verdict=common_pb2.VERDICT_BEHAVIORAL_REGRESSION,
        diff=replay_pb2.Diff(has_behavioral_change=True, fields=[field]),
    )


def error(scenario_id: str, message: str = "boom") -> object:
    return replay_pb2.ReplayResult(
        scenario_id=scenario_id, verdict=common_pb2.VERDICT_ERROR, error=message
    )


def perf(scenario_id: str, delta: int = 50) -> object:
    return replay_pb2.ReplayResult(
        scenario_id=scenario_id, verdict=common_pb2.VERDICT_PERF_REGRESSION, latency_delta_ms=delta
    )


def test_collapses_duplicates_and_ranks() -> None:
    results: list[object] = []
    results += [behavioral(f"t{i}", "order.total") for i in range(5)]
    results += [behavioral(f"s{i}", f"items[{i}].sku") for i in range(3)]  # near-dups
    results += [error("e1"), error("e2")]
    results += [perf("p1")]
    results.append(replay_pb2.ReplayResult(scenario_id="ok", verdict=common_pb2.VERDICT_PASS))

    explanations = explain_failures(results)

    # 11 failures collapse to 4 explanations (a PASS is ignored).
    assert len(explanations) == 4

    severities = [e.severity for e in explanations]
    assert severities == sorted(severities, reverse=True)

    # Errors are the most severe.
    assert explanations[0].likely_cause == "request error"
    assert set(explanations[0].scenario_ids) == {"e1", "e2"}

    total = next(e for e in explanations if "order.total" in e.title)
    assert len(total.scenario_ids) == 5

    sku = next(e for e in explanations if "items[].sku" in e.title)
    assert len(sku.scenario_ids) == 3


def test_status_code_change_cause() -> None:
    result = behavioral("a", "status_code", baseline="200", candidate="500")
    explanations = explain_failures([result])
    assert explanations[0].likely_cause == "status code change"


def test_removed_field_ranks_above_added() -> None:
    removed = behavioral("r", "coupon", kind=replay_pb2.DIFF_KIND_REMOVED)
    added = behavioral("a", "banner", kind=replay_pb2.DIFF_KIND_ADDED)
    explanations = explain_failures([added, removed])
    assert explanations[0].likely_cause == "removed field"
