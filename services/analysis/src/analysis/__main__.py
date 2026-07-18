"""Run the analysis service.

Phase 0: a minimal stdlib HTTP health endpoint so the service builds, runs, and
is wired into docker-compose. The gRPC analysis server lands in Phase 6.
"""

from __future__ import annotations

import json
import os
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

from analysis.health import report


class _Handler(BaseHTTPRequestHandler):
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


def main() -> None:
    port = int(os.environ.get("PORT", "8082"))
    server = ThreadingHTTPServer(("", port), _Handler)
    print(f"analysis listening on :{port}", flush=True)
    server.serve_forever()


if __name__ == "__main__":
    main()
