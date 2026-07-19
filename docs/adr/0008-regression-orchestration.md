# ADR 0008: Regression run orchestration and GitHub intake

- **Status:** Accepted
- **Date:** 2026-07-18

## Context

A pull request must trigger a regression run: provision an environment, replay
scenarios via the Go engine, decide pass/fail, and report back to GitHub. We
need a design that is secure at the edge, resilient to failure, and testable
without real Kubernetes or GitHub.

## Decision

- **Webhook intake.** `POST /api/v1/webhooks/github` is anonymous but verified
  by the GitHub HMAC-SHA256 signature (constant-time compare). It handles
  `pull_request` `opened`/`synchronize`/`reopened`, maps the repo to a project,
  and is **idempotent** per `(project, PR number, commit SHA)`.
- **Async orchestration.** The webhook creates a `RegressionRun` (Pending),
  enqueues it on an in-process queue, and returns `202`. A `BackgroundService`
  dequeues and runs the state machine (Provisioning → Replaying → Analyzing →
  Completed/Failed), persisting status transitions and audit entries.
- **External dependencies behind interfaces.** `IEnvironmentProvisioner`,
  `IReplayClient` (gRPC to the Go engine), and `IGitHubChecks` isolate the
  outside world. Phase 5 ships a placeholder provisioner (config-derived URL)
  and a commit-status GitHub client that logs when unconfigured; the real GKE
  provisioner lands in Phase 8. Tests substitute in-memory fakes.
- **Verdict → check.** Any behavioral/perf/error result fails the run; the
  posted commit status links to the dashboard report.

## Consequences

- The webhook responds fast and survives retries (idempotent); duplicate
  deliveries never double-run.
- The orchestration flow is identical whether the environment is real or
  placeholder, so Phase 8 only swaps the provisioner.
- The in-process queue is not durable — a crash mid-run loses queued work. A
  future revision may move to a persistent queue / outbox if needed.

## Alternatives considered

- **Synchronous orchestration in the webhook:** rejected — replay can take
  minutes; GitHub expects a fast 2xx.
- **Durable queue (Pub/Sub) now:** deferred — in-process is enough until we run
  multiple API replicas; the `IRunQueue` seam makes the swap local.
