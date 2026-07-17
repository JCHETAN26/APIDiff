"""Health reporting for the analysis service."""

from __future__ import annotations

from dataclasses import asdict, dataclass

from analysis import __version__


@dataclass(frozen=True)
class Status:
    """Service health state reported over HTTP and (later) gRPC."""

    service: str
    status: str
    version: str


def report() -> dict[str, str]:
    """Return the current health status as a serializable mapping."""
    return asdict(
        Status(service="analysis", status="ok", version=__version__)
    )
