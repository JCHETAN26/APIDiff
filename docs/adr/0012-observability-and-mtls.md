# ADR 0012: Observability and service-to-service mTLS

- **Status:** Accepted
- **Date:** 2026-07-20

## Context

Phase 9 hardens the platform: a run must be traceable end-to-end across four
runtimes, we need SLOs/metrics, and internal gRPC traffic must be encryptable and
authenticated. This has to work without forcing every developer to run a
collector or a PKI locally.

## Decision

- **OpenTelemetry everywhere.** `api` (C#), `replay-engine` (Go), and `analysis`
  (Python) each initialize the OTel SDK and export **OTLP/gRPC** to
  `OTEL_EXPORTER_OTLP_ENDPOINT`. When that variable is unset, the SDK is
  installed but exports nothing — a no-op for tests and local runs. `docker
  compose` includes a collector that logs to console.
- **Correlation by `run_id`.** The orchestrator opens one `regression.run` span
  tagged with `apidiff.run_id`; W3C trace context propagates over gRPC, so one
  run is one distributed trace. A `apidiff.runs.completed{outcome}` counter backs
  the run-success SLO.
- **mTLS-capable gRPC.** The Go replay server loads TLS from `TLS_CERT_FILE` /
  `TLS_KEY_FILE`, and requires+verifies client certs when `TLS_CLIENT_CA_FILE` is
  set (TLS 1.3). Unset ⇒ plaintext, for local dev and for clusters where the mesh
  terminates mTLS. The toggle is env-driven; cert issuance/rotation is
  environment config (cert-manager or mesh), not code.
- **SLOs, threat model, runbook** are documented (`docs/OBSERVABILITY.md`,
  `THREAT_MODEL.md`, `RUNBOOK.md`).

## Consequences

- One trace per run makes stuck/failed runs diagnosable across languages.
- No collector or PKI is required to build, test, or run locally; production
  wires the OTLP endpoint and certificates via config.
- The NuGet audit gate (warnings-as-errors) already blocked a vulnerable OTLP
  exporter version, forcing an upgrade to a patched release — the security gate
  working as intended.

## Alternatives considered

- **App-managed mTLS as the only option:** rejected — many deployments prefer a
  mesh (Istio/Linkerd) for mTLS; the env toggle supports both without forcing one.
- **Vendor-specific tracing SDKs:** rejected — OTLP keeps the backend swappable
  (Tempo/Jaeger/Cloud Trace) behind the collector.
