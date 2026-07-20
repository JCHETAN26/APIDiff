# ADR 0011: Deployment topology and CI/CD

- **Status:** Accepted
- **Date:** 2026-07-20

## Context

The services need reproducible infrastructure and an automated path from a merge
to a running staging environment, plus the security scanning the build plan
calls for. Real GCP credentials are configured out of band, so the pipeline must
be safe to have in the repo before it is wired up.

## Decision

- **Infrastructure as code.** Terraform provisions GCP (VPC, GKE with Workload
  Identity, Cloud SQL private-IP Postgres, Artifact Registry, Secret Manager,
  IAM). CI runs `fmt`/`init -backend=false`/`validate` on every infra change.
- **Kubernetes via Helm.** One chart renders all four services from a `services`
  map (DRY). The **same chart** serves the ephemeral per-PR environment via a
  `values-pr.yaml` overlay in an isolated namespace — so the production and
  candidate paths are identical (ADR 0004). CI lints and renders the chart.
- **Image build + deploy.** On push to `main`, `deploy.yml` authenticates to GCP
  with **Workload Identity Federation** (no long-lived keys), builds and pushes
  images to Artifact Registry, and `helm upgrade --install --wait`s to staging.
  `--wait` is the health gate; a failed rollout triggers `helm rollback` — the
  canary/rollback strategy.
- **Guarded by default.** The deploy job runs only when the repo variable
  `DEPLOY_ENABLED=true` and the GCP secrets/variables are set, so `main` stays
  green until deployment is configured.
- **Security scanning.** `security.yml` runs Trivy on every PR: committed
  **secrets fail** the build (test-fixture dir excluded — it holds deliberate
  fakes); dependency and IaC-misconfiguration findings are reported for now.

## Consequences

- A configured repo deploys to staging automatically on merge, reproducibly from
  `terraform apply` + `helm upgrade`.
- No cloud credentials live in the repo; WIF issues short-lived tokens.
- The Trivy vuln/misconfig gates start non-blocking; they can be tightened to
  `--exit-code 1` once the baseline is clean.

## Alternatives considered

- **Argo Rollouts / Flagger for canary:** deferred — `helm --wait` + rollback
  covers the current need without extra cluster components.
- **Service-account JSON keys:** rejected in favor of Workload Identity
  Federation (no static secrets).
