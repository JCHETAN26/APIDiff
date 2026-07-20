"""OpenTelemetry tracing for the analysis service."""

from __future__ import annotations

import os
from typing import Any

_SERVICE_NAME = "apidiff-analysis"


def server_interceptors() -> list[Any]:
    """Return gRPC server interceptors for tracing (empty if OTel is unavailable)."""
    try:
        from opentelemetry.instrumentation.grpc import server_interceptor
    except ImportError:
        return []
    return [server_interceptor()]  # type: ignore[no-untyped-call]


def setup_tracing() -> None:
    """Configure the global tracer provider.

    Exports over OTLP when OTEL_EXPORTER_OTLP_ENDPOINT is set; otherwise installs
    a plain provider so the service runs without a collector. Missing OTel
    packages are tolerated so the service still starts.
    """
    try:
        from opentelemetry import trace
        from opentelemetry.sdk.resources import Resource
        from opentelemetry.sdk.trace import TracerProvider
    except ImportError:
        return

    provider = TracerProvider(resource=Resource.create({"service.name": _SERVICE_NAME}))

    if os.environ.get("OTEL_EXPORTER_OTLP_ENDPOINT"):
        from opentelemetry.exporter.otlp.proto.grpc.trace_exporter import OTLPSpanExporter
        from opentelemetry.sdk.trace.export import BatchSpanProcessor

        provider.add_span_processor(BatchSpanProcessor(OTLPSpanExporter()))

    trace.set_tracer_provider(provider)
