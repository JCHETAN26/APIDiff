# infra/deploy

Kubernetes manifests / Helm charts for APIDiff (Phase 8):

- base services (`api`, `replay-engine`, `analysis`, `dashboard`)
- the ephemeral **per-PR** test environment template used by regression runs

Each service exposes `/healthz` for HTTP liveness/readiness probes.
