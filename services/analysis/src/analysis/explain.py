"""Explain and rank replay failures, collapsing duplicates into a shortlist."""

from __future__ import annotations

import math
from collections import defaultdict
from typing import Any

from apidiff.analysis.v1 import analysis_pb2
from apidiff.common.v1 import common_pb2
from apidiff.replay.v1 import replay_pb2

from analysis.normalize import normalize_field_path

_KIND_VERB = {
    replay_pb2.DIFF_KIND_ADDED: "added",
    replay_pb2.DIFF_KIND_REMOVED: "removed",
    replay_pb2.DIFF_KIND_CHANGED: "changed",
}
_KIND_BASE = {
    replay_pb2.DIFF_KIND_ADDED: 0.4,
    replay_pb2.DIFF_KIND_REMOVED: 0.8,
    replay_pb2.DIFF_KIND_CHANGED: 0.6,
}

# Signature: a hashable key grouping failures that share the same root cause.
Signature = tuple[Any, ...]


def explain_failures(results: list[Any]) -> list[Any]:
    """Group failing results by cause and return explanations, most severe first."""
    groups: dict[Signature, list[Any]] = defaultdict(list)
    for result in results:
        if result.verdict in (common_pb2.VERDICT_PASS, common_pb2.VERDICT_UNSPECIFIED):
            continue
        groups[_signature(result)].append(result)

    explanations = [_explain(signature, group) for signature, group in groups.items()]
    explanations.sort(key=lambda e: e.severity, reverse=True)
    return explanations


def _signature(result: Any) -> Signature:
    if result.verdict == common_pb2.VERDICT_ERROR:
        return ("error",)
    if result.verdict == common_pb2.VERDICT_PERF_REGRESSION:
        return ("perf",)
    fields = tuple(sorted((normalize_field_path(f.path), int(f.kind)) for f in result.diff.fields))
    return ("behavioral", fields)


def _explain(signature: Signature, group: list[Any]) -> Any:
    count = len(group)
    scenario_ids = sorted(r.scenario_id for r in group)
    kind = signature[0]

    if kind == "error":
        title = f"{count} scenario(s) returned an error"
        detail = "Requests failed during replay: " + (group[0].error or "unknown error")
        cause = "request error"
        base = 1.0
    elif kind == "perf":
        avg = sum(r.latency_delta_ms for r in group) / count
        title = f"Latency regressed in {count} scenario(s)"
        detail = f"Candidate was slower by {avg:.0f} ms on average."
        cause = "performance regression"
        base = 0.5
    else:
        title, detail, cause, base = _describe_behavioral(signature[1], count, group)

    # Base cause dominates ranking; more affected scenarios raise it slightly.
    severity = min(1.0, base * (1 + 0.2 * math.log10(count)))
    return analysis_pb2.FailureExplanation(
        title=title,
        detail=detail,
        scenario_ids=scenario_ids,
        severity=severity,
        likely_cause=cause,
    )


def _describe_behavioral(
    fields: tuple[tuple[str, int], ...], count: int, group: list[Any]
) -> tuple[str, str, str, float]:
    if len(fields) == 1:
        path, kind = fields[0]
        verb = _KIND_VERB.get(kind, "changed")
        if path == "status_code":
            sample = _sample_field(group, "status_code")
            title = f"Status code changed in {count} scenario(s)"
            detail = f"Status code {sample}." if sample else "Response status code changed."
            return title, detail, "status code change", 0.9
        title = f"Field `{path}` {verb} in {count} scenario(s)"
        detail = _field_detail(path, verb, group)
        return title, detail, f"{verb} field", _KIND_BASE.get(kind, 0.6)

    summary = ", ".join(f"{path} ({_KIND_VERB.get(kind, 'changed')})" for path, kind in fields[:5])
    title = f"{len(fields)} fields changed in {count} scenario(s)"
    detail = f"Changed fields: {summary}."
    return title, detail, "response schema change", 0.7


def _field_detail(path: str, verb: str, group: list[Any]) -> str:
    sample = _sample_field(group, path)
    if sample:
        return f"Field `{path}` {verb}: {sample}."
    return f"Field `{path}` {verb} across {len(group)} scenario(s)."


def _sample_field(group: list[Any], normalized_path: str) -> str | None:
    for field in group[0].diff.fields:
        if normalize_field_path(field.path) == normalized_path:
            return f"{field.baseline_value or '∅'} → {field.candidate_value or '∅'}"
    return None
