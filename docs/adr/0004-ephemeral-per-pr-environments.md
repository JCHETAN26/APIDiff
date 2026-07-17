# ADR 0004: Ephemeral per-PR Kubernetes environments

- **Status:** Accepted
- **Date:** 2026-07-17

## Context

A regression run replays real traffic against a pull-request build. Runs must be
isolated (no shared mutable state), reproducible, and safe to run concurrently
for many open PRs.

## Decision

Each regression run provisions an ephemeral, sandboxed Kubernetes environment
containing the **candidate** (PR) build. The **baseline** is the current default
environment. The ephemeral environment is torn down when the run completes.

## Consequences

- Runs are isolated and concurrency-safe; one PR cannot corrupt another's state.
- Environments are reproducible from the PR's build artifacts.
- Requires namespace/resource provisioning and teardown automation (Phase 5) and
  cost controls (TTL, quotas).

## Alternatives considered

- **Shared long-lived test environment:** cheaper but serializes runs and risks
  cross-run interference; rejected.
- **Two fresh environments per run (baseline + candidate):** cleanest
  comparison but doubles cost and time; revisit if baseline drift becomes a
  problem.
