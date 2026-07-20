"""gRPC AnalysisService implementation."""

from __future__ import annotations

from typing import Any

from apidiff.analysis.v1 import analysis_pb2, analysis_pb2_grpc

from analysis.clustering import cluster_scenarios
from analysis.explain import explain_failures


class AnalysisService(analysis_pb2_grpc.AnalysisServiceServicer):  # type: ignore[misc]
    """Clusters duplicate scenarios and explains replay failures."""

    def ClusterScenarios(self, request: Any, context: Any) -> Any:  # noqa: N802 (gRPC name)
        clusters = cluster_scenarios(request.project_id, list(request.scenarios))
        return analysis_pb2.ClusterScenariosResponse(clusters=clusters)

    def ExplainFailures(self, request: Any, context: Any) -> Any:  # noqa: N802 (gRPC name)
        explanations = explain_failures(list(request.failures))
        return analysis_pb2.ExplainFailuresResponse(explanations=explanations)
