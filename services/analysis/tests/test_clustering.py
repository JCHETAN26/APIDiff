from apidiff.analysis.v1 import analysis_pb2

from analysis.clustering import cluster_scenarios


def desc(scenario_id: str, method: str, path: str) -> object:
    return analysis_pb2.ScenarioDescriptor(
        scenario_id=scenario_id, method=method, path=path, fingerprint=""
    )


def test_clusters_by_normalized_path() -> None:
    descriptors = [
        desc("s2", "GET", "/orders/2"),
        desc("s1", "GET", "/orders/1"),
        desc("s3", "POST", "/orders/1"),
        desc("s4", "GET", "/users/5"),
    ]

    clusters = cluster_scenarios("proj", descriptors)
    by_members = {tuple(c.member_scenario_ids): c for c in clusters}

    assert len(clusters) == 3
    get_orders = by_members[("s1", "s2")]
    assert get_orders.representative_scenario_id == "s1"
    assert ("s3",) in by_members
    assert ("s4",) in by_members


def test_deterministic_cluster_id() -> None:
    descriptors = [desc("a", "GET", "/x/1")]
    first = cluster_scenarios("p", descriptors)[0].cluster_id
    second = cluster_scenarios("p", descriptors)[0].cluster_id
    assert first == second and first != ""
