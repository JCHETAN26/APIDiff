from concurrent import futures
from typing import Any

import grpc
from apidiff.analysis.v1 import analysis_pb2, analysis_pb2_grpc
from apidiff.common.v1 import common_pb2
from apidiff.replay.v1 import replay_pb2

from analysis.service import AnalysisService


def _start_server() -> tuple[Any, int]:
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=2))
    analysis_pb2_grpc.add_AnalysisServiceServicer_to_server(AnalysisService(), server)
    port = server.add_insecure_port("[::]:0")
    server.start()
    return server, port


def test_grpc_roundtrip() -> None:
    server, port = _start_server()
    try:
        with grpc.insecure_channel(f"localhost:{port}") as channel:
            stub = analysis_pb2_grpc.AnalysisServiceStub(channel)

            cluster_req = analysis_pb2.ClusterScenariosRequest(project_id="p")
            cluster_req.scenarios.add(scenario_id="s1", method="GET", path="/o/1")
            cluster_req.scenarios.add(scenario_id="s2", method="GET", path="/o/2")
            cluster_resp = stub.ClusterScenarios(cluster_req)
            assert len(cluster_resp.clusters) == 1
            assert set(cluster_resp.clusters[0].member_scenario_ids) == {"s1", "s2"}

            explain_resp = stub.ExplainFailures(
                analysis_pb2.ExplainFailuresRequest(
                    run_id="r",
                    failures=[
                        replay_pb2.ReplayResult(
                            scenario_id="e1", verdict=common_pb2.VERDICT_ERROR, error="upstream 500"
                        )
                    ],
                )
            )
            assert len(explain_resp.explanations) == 1
            assert explain_resp.explanations[0].likely_cause == "request error"
    finally:
        server.stop(grace=None)
