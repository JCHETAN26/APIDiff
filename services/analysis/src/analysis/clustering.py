"""Cluster duplicate / near-duplicate scenarios so a run replays representatives."""

from __future__ import annotations

import hashlib
from collections import defaultdict
from typing import Any

from apidiff.analysis.v1 import analysis_pb2

from analysis.normalize import normalize_path


def _cluster_id(project_id: str, method: str, normalized_path: str) -> str:
    digest = hashlib.sha1(f"{project_id}|{method}|{normalized_path}".encode())
    return digest.hexdigest()[:16]


def cluster_scenarios(project_id: str, descriptors: list[Any]) -> list[Any]:
    """Group scenarios by (method, normalized path); return one cluster each.

    The representative is the lexicographically smallest scenario id, making the
    result deterministic.
    """
    groups: dict[tuple[str, str], list[str]] = defaultdict(list)
    for descriptor in descriptors:
        key = (descriptor.method.upper(), normalize_path(descriptor.path))
        groups[key].append(descriptor.scenario_id)

    clusters: list[Any] = []
    for (method, normalized_path), member_ids in groups.items():
        ordered = sorted(member_ids)
        clusters.append(
            analysis_pb2.ScenarioCluster(
                cluster_id=_cluster_id(project_id, method, normalized_path),
                representative_scenario_id=ordered[0],
                member_scenario_ids=ordered,
            )
        )

    clusters.sort(key=lambda c: c.cluster_id)
    return clusters
