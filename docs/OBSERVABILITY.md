# Observability & SLOs

> Status: Phase 9 · Companion to [ARCHITECTURE.md](./ARCHITECTURE.md).

## Telemetry

All four services emit OpenTelemetry:

| Service | Signals | Instrumentation |
|---|---|---|
| `api` (C#) | traces, metrics | ASP.NET Core, HttpClient, gRPC client, custom run span/metrics |
| `replay-engine` (Go) | traces | `otelgrpc` server handler, SDK |
| `analysis` (Python) | traces | gRPC server interceptor, SDK |
| `dashboard` | — | (browser RUM out of scope) |

Export is via **OTLP/gRPC** to the endpoint in `OTEL_EXPORTER_OTLP_ENDPOINT`.
When unset, the SDK is installed but exports nothing — a no-op for local runs and
tests. `docker compose` runs an OpenTelemetry Collector (`infra/otel/collector.yaml`)
that receives OTLP and logs to the console; point the collector's exporters at
Tempo/Jaeger/Cloud Trace in staging and prod.

### End-to-end correlation by `run_id`

The orchestrator opens one span (`regression.run`) per run, tagged with
`apidiff.run_id`, `apidiff.project_id`, and `apidiff.pr_number`. Trace context
propagates over gRPC to the replay engine and analysis service (W3C
`traceparent`), so a single run is one distributed trace across C#, Go, and
Python. The `apidiff.runs.completed` counter is tagged by outcome
(`success` / `regression` / `failed`).

## Service Level Objectives

| SLO | Target | Signal |
|---|---|---|
| API availability | 99.5% of `/api/v1` requests non-5xx (30-day) | ASP.NET Core metrics |
| API latency | p95 read requests < 300 ms | http.server.duration |
| Webhook intake | p95 `POST /webhooks/github` < 500 ms | http.server.duration |
| Run success rate | ≥ 95% of runs reach a terminal state without `failed` | `apidiff.runs.completed{outcome}` |
| Replay throughput | sustain ≥ 100 scenarios/s at target concurrency | load test (below) |

### Error budget & alerting

- **Availability**: page when the 1-hour 5xx rate burns >2% of the 30-day budget
  (fast burn); ticket on slow burn.
- **Run failures**: alert when `outcome="failed"` exceeds 5% over 1 hour.
- **Saturation**: alert on node CPU > 80% or Cloud SQL connections > 80% for
  10 minutes.

Alert rules are defined against the collector's Prometheus/metrics backend
(wired per environment; not committed).

## Load & soak testing

- **Unit-level load**: `services/replay-engine` includes a 200-scenario
  concurrency test asserting no goroutine leak (`go test -race`).
- **Service-level**: run the replay engine against a stub target and drive the
  gRPC `Replay` RPC with a scenario batch sized to the throughput SLO; watch the
  `apidiff.runs.*` metrics and goroutine/heap profiles. Soak for ≥ 1 hour before
  a release to catch slow leaks.
