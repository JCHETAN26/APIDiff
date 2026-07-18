# ADR 0001: Single monorepo for all services

- **Status:** Accepted
- **Date:** 2026-07-17

## Context

APIDiff spans four runtimes (C#, Go, Python, TypeScript) plus infra
(Terraform, Kubernetes). These services share gRPC contracts and evolve
together. We must choose between one repository and per-service repositories.

## Decision

Use a single monorepo with top-level `proto/`, `services/`, `web/`, `infra/`,
and `.github/` directories.

## Consequences

- Cross-service changes (e.g. a `.proto` change and its consumers) land in one
  atomic PR.
- One CI pipeline; path filters run only the jobs affected by a change.
- Shared conventions (formatting, commit style, review rules) live in one place.
- The tradeoff: CI must be path-aware to avoid rebuilding everything, and the
  repo mixes toolchains — mitigated by per-service tooling configs.

## Alternatives considered

- **Polyrepo:** stronger isolation and independent versioning, but coordinating
  contract changes across repos is slow and error-prone for a small team.
