# proto — gRPC contracts

Source of truth for every cross-service boundary (C# ↔ Go ↔ Python).

Phase 1 adds the actual service definitions:

- `apidiff/replay/v1/replay.proto` — API → replay-engine
- `apidiff/analysis/v1/analysis.proto` — API → analysis
- `apidiff/common/v1/*.proto` — shared messages

Stub generation for C#, Go, and Python is wired into CI. Whether generated code
is committed or regenerated in CI is decided in an ADR under `docs/adr/`.

Lint/breaking-change checks use [buf](https://buf.build) (`buf lint`,
`buf breaking`).
