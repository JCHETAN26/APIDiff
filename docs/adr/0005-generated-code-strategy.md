# ADR 0005: How generated gRPC code is produced and stored

- **Status:** Accepted
- **Date:** 2026-07-17

## Context

The `.proto` files in `proto/` are the source of truth (ADR 0002). Each of the
three services needs language stubs. We must decide, per language, whether
stubs are committed to the repo or generated at build time, and how CI keeps
them honest.

## Decision

- **Go** (`replay-engine`) and **Python** (`analysis`): generate with `buf` and
  **commit** the output under `services/*/gen/`. Go and Python builds are then
  hermetic and need no protoc/buf at build time.
- **C#** (`api`): generate **at build time** via `Grpc.Tools`, which references
  the shared `.proto` files from the csproj. This is the idiomatic .NET flow;
  nothing is committed.
- **CI** runs `buf lint`, `buf breaking` (against `main`), and a **drift
  check**: it re-runs `buf generate` and fails if the committed Go/Python stubs
  differ. C# codegen is exercised by simply building the `api` project.

## Consequences

- A `.proto` change that isn't regenerated fails CI (drift check), satisfying
  the Phase 1 goal that contract changes regenerate stubs.
- Reviewers see generated Go/Python diffs in PRs (noise), traded for hermetic
  builds and no network dependency on the buf schema registry at build time.
- Generated directories are excluded from linters/formatters and coverage.

## Alternatives considered

- **Regenerate all three in CI, commit nothing:** cleaner diffs but every build
  job needs buf + network, and IDEs can't resolve symbols without a manual
  generate step.
- **Commit C# too:** rejected — `Grpc.Tools` build-time generation is the .NET
  norm and keeps the C# tree clean.
