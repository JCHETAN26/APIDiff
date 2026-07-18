# ADR 0007: Replay comparison and verdict semantics

- **Status:** Accepted
- **Date:** 2026-07-18

## Context

The Go replay engine replays each scenario against a baseline and a candidate,
then must decide whether the candidate regressed. We need clear, deterministic
rules for what counts as a difference and how a verdict is assigned.

## Decision

- **Baseline source.** If a live baseline target is supplied, it is replayed;
  otherwise the scenario's captured `reference_response` is used as the
  baseline. This supports both live A/B replay and replay-against-capture.
- **Diff scope.** Compare HTTP status code and response body. Bodies that parse
  as JSON get a structural + value diff (added / removed / changed leaves);
  non-JSON bodies get a raw byte comparison. Response headers are not diffed by
  default (too noisy — Date, request ids, etc.).
- **Ignore rules.** `ignore_fields` entries match either a full dot/index path
  (`data.createdAt`) or a bare leaf name (`timestamp`) at any depth, letting
  callers suppress volatile fields.
- **Verdict precedence (first match wins):** `ERROR` if either request failed →
  `BEHAVIORAL_REGRESSION` if any diff exists → `PERF_REGRESSION` if candidate
  latency exceeds baseline by more than `latency_regression_ratio` (default
  0.20) → otherwise `PASS`.
- **Concurrency.** A bounded worker pool (default 8) processes scenarios;
  results stream back through a single goroutine, so backpressure from the gRPC
  stream naturally throttles the workers.

## Consequences

- Verdicts are deterministic and explained by the attached field diffs.
- Behavioral changes take precedence over performance, so a regression is never
  masked by also being slower.
- Header regressions are currently invisible; a future revision may add an
  opt-in header allowlist to diff.

## Alternatives considered

- **Diff everything including headers:** rejected for now — dominated by
  volatile values; would require an allowlist to be useful.
- **Element-wise array diffing with LCS:** deferred — arrays whose lengths
  differ are reported as a single change; index-wise diffing covers equal-length
  arrays, which is the common case.
