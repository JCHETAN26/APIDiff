# Threat Model & Security Review

> Status: Phase 9 · Scope: the APIDiff platform (not customer apps under test).

## Assets

- **Sanitized captured traffic** (scenarios) — may still resemble production
  shapes; must not leak.
- **Regression results / diffs** — reveal internal API behavior.
- **Credentials & secrets** — DB connection string, GitHub webhook secret and
  token, OIDC signing keys, cloud IAM.
- **Audit log** — integrity matters for forensics.

## Trust boundaries

1. **Internet → API**: GitHub webhooks and dashboard/OIDC users.
2. **API → data plane**: gRPC to the Go/Python services inside the cluster.
3. **Cluster → GCP**: Cloud SQL, Secret Manager, Artifact Registry.
4. **Per-PR ephemeral env**: runs candidate code — treated as untrusted.

## STRIDE summary & mitigations

| Threat | Vector | Mitigation |
|---|---|---|
| **Spoofing** | Forged webhooks | HMAC-SHA256 verification, constant-time compare ([ADR 0008]) |
| **Spoofing** | Unauthenticated API | OIDC + JWT bearer; RBAC per org ([Phase 2]) |
| **Tampering** | In-cluster gRPC MITM | mTLS-capable gRPC (TLS 1.3, client-cert verify) ([ADR 0012]) |
| **Repudiation** | "I didn't do that" | Append-only audit log committed atomically with each mutation |
| **Info disclosure** | PII/secrets at rest | Sanitize-at-ingest; golden tests assert no secret survives ([ADR 0006]) |
| **Info disclosure** | Secrets in images/env | GCP Secret Manager + Workload Identity; no static keys; Trivy secret scan in CI |
| **DoS** | Replay amplification | Bounded worker pool + per-request timeouts ([ADR 0007]); run concurrency limits |
| **Elevation** | Candidate code escaping | Ephemeral, namespace-isolated per-PR env, torn down after the run ([ADR 0004]) |

## Supply chain

- Trivy scans dependencies and IaC on every PR; committed secrets **fail** the
  build. NuGet audit is `TreatWarningsAsErrors` (a vulnerable transitive fails
  the C# build — this caught and blocked a vulnerable OTLP exporter version).
- Container base images are pinned; distroless/slim where possible.
- Generated gRPC stubs are drift-checked in CI ([ADR 0005]).

## Residual risks / follow-ups

- Sanitization is pattern-based and can miss novel secret formats — revisit
  rules per project; consider a deny-by-default body policy for sensitive routes.
- mTLS is available but certificate issuance/rotation (cert-manager or mesh) is
  environment config, not yet automated here.
- The pasted-token dashboard sign-in is a dev affordance and must be replaced by
  the OIDC flow before GA ([ADR 0010]).
- In-process run queue is not durable; a crash mid-run loses queued work
  ([ADR 0008]).
