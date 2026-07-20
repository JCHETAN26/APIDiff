"""gRPC server bootstrap for the analysis service."""

from __future__ import annotations

from concurrent import futures
from typing import Any

import grpc
from apidiff.analysis.v1 import analysis_pb2_grpc

from analysis.observability import server_interceptors
from analysis.service import AnalysisService


def create_server(port: int, max_workers: int = 10) -> Any:
    """Build (but do not start) a gRPC server bound to the given port."""
    server = grpc.server(
        futures.ThreadPoolExecutor(max_workers=max_workers),
        interceptors=server_interceptors(),
    )
    analysis_pb2_grpc.add_AnalysisServiceServicer_to_server(AnalysisService(), server)
    server.add_insecure_port(f"[::]:{port}")
    return server
