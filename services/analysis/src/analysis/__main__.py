"""Run the analysis service: gRPC AnalysisService plus an HTTP health endpoint."""

from __future__ import annotations

import json
import os
import threading
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

from analysis.health import report
from analysis.observability import setup_tracing
from analysis.server import create_server


class _HealthHandler(BaseHTTPRequestHandler):
    def do_GET(self) -> None:  # noqa: N802 (stdlib-defined name)
        if self.path != "/healthz":
            self.send_error(404)
            return
        body = json.dumps(report()).encode()
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def log_message(self, *_args: object) -> None:
        """Silence the default stderr access log."""


def _serve_health(port: int) -> None:
    ThreadingHTTPServer(("", port), _HealthHandler).serve_forever()


def main() -> None:
    grpc_port = int(os.environ.get("GRPC_PORT", "9091"))
    http_port = int(os.environ.get("PORT", "8082"))

    setup_tracing()
    server = create_server(grpc_port)
    server.start()

    threading.Thread(target=_serve_health, args=(http_port,), daemon=True).start()

    print(f"analysis: gRPC on :{grpc_port}, health on :{http_port}", flush=True)
    server.wait_for_termination()


if __name__ == "__main__":
    main()
