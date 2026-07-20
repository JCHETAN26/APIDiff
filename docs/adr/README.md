# Architecture Decision Records

Non-obvious decisions are recorded here as ADRs. Copy
[`0000-adr-template.md`](./0000-adr-template.md), increment the number, and open
it in the PR that makes the decision.

| ADR | Title | Status |
|-----|-------|--------|
| [0001](./0001-monorepo.md) | Single monorepo for all services | Accepted |
| [0002](./0002-grpc-contract-first.md) | gRPC, contract-first, for service-to-service | Accepted |
| [0003](./0003-postgres-system-of-record.md) | PostgreSQL as the single system of record | Accepted |
| [0004](./0004-ephemeral-per-pr-environments.md) | Ephemeral per-PR Kubernetes environments | Accepted |
| [0005](./0005-generated-code-strategy.md) | How generated gRPC code is produced and stored | Accepted |
| [0006](./0006-capture-time-sanitization.md) | Sanitize captured traffic at ingest, before storage | Accepted |
| [0007](./0007-replay-diff-verdict.md) | Replay comparison and verdict semantics | Accepted |
| [0008](./0008-regression-orchestration.md) | Regression run orchestration and GitHub intake | Accepted |
| [0009](./0009-analysis-heuristics.md) | Scenario clustering and failure-explanation heuristics | Accepted |
| [0010](./0010-dashboard-data-and-auth.md) | Dashboard data fetching and authentication | Accepted |
