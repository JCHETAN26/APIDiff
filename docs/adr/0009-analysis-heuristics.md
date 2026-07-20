# ADR 0009: Scenario clustering and failure-explanation heuristics

- **Status:** Accepted
- **Date:** 2026-07-18

## Context

A regression run can produce many failures that are really the same problem
(the same field changed across hundreds of near-identical requests). The
analysis service must collapse that noise into a ranked, explained shortlist and
cluster duplicate scenarios so runs replay representatives.

## Decision

- **Clustering** groups scenarios by `(method, normalized path)`, where the path
  normalizer replaces numeric, UUID, and long-hex segments with placeholders
  (`/orders/123` → `/orders/{id}`). The representative is the lexicographically
  smallest scenario id (deterministic); the cluster id is a stable hash of the
  key.
- **Failure explanation** assigns each failure a *signature*:
  - `ERROR` → grouped as request errors,
  - `PERF_REGRESSION` → grouped as latency regressions,
  - `BEHAVIORAL` → grouped by the set of `(normalized field path, kind)` changes,
    where array indices are collapsed (`items[0].sku` → `items[].sku`).
  Failures sharing a signature become one `FailureExplanation` listing all
  affected scenario ids.
- **Ranking.** Severity is `base × (1 + 0.2·log10(count))`, capped at 1.0, where
  `base` reflects the cause (error 1.0 > status-code 0.9 > removed 0.8 >
  changed 0.6 > perf 0.5 > added 0.4). The cause dominates; the number of
  affected scenarios is a tiebreaker. Explanations are returned most-severe
  first.

## Consequences

- Hundreds of duplicate failures collapse to a handful of explanations, so
  reviewers see the highest-signal regressions first.
- Heuristics are deterministic and unit-testable, with no ML dependency.
- They are approximate: unusual path schemes or field structures may cluster
  imperfectly. The rules live in one module (`normalize`, `explain`) for tuning.

## Alternatives considered

- **Embedding/ML clustering:** rejected for now — heavier, non-deterministic,
  and unnecessary for the structured diffs we already have.
- **Exact-fingerprint-only clustering:** too strict — misses near-duplicates
  that differ only by ids in the path.
