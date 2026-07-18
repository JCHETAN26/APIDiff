# ADR 0006: Sanitize captured traffic at ingest, before storage

- **Status:** Accepted
- **Date:** 2026-07-18

## Context

APIDiff replays real staging traffic, which contains PII and secrets. That data
must never land in durable storage (Postgres) or flow to the replay/analysis
services. We need a clear point and policy for redaction.

## Decision

Redact at **capture ingest**, synchronously, before the scenario is persisted
(design principle "sanitize at the edge"). The `Sanitizer` applies:

- **Headers:** an allowlist — only known-safe headers are kept; everything else
  (Authorization, Cookie, X-Api-Key, …) is dropped. Kept values are still
  pattern-scrubbed.
- **Bodies:** dispatched by content type — JSON is walked recursively (sensitive
  keys redacted wholesale, string values pattern-scrubbed); form-urlencoded is
  treated like a query; other text is pattern-scrubbed.
- **Query strings:** sensitive params redacted, values pattern-scrubbed.
- **Value patterns:** email, JWT, bearer token, credit-card, US SSN, AWS key.

Redaction replaces content with `[REDACTED]`. The stored `Fingerprint` is
computed over sanitized data so equivalent requests still cluster.

## Consequences

- Raw sensitive data never reaches Postgres or downstream services.
- The policy is centralized and unit-testable; a golden test asserts a battery
  of secrets never survives, and an integration test asserts nothing sensitive
  reaches the database.
- Redaction is lossy and pattern-based: it can over-redact (e.g. a 12–16 digit
  order id) or miss novel secret formats. `SanitizationOptions` is the single
  place to tune rules; future work may add per-project custom rules.

## Alternatives considered

- **Store raw, redact on read:** rejected — raw secrets at rest is exactly the
  risk we must avoid, and audit/backup copies would leak.
- **Redact in the Go replay engine:** rejected — capture is the earliest choke
  point and keeps secrets out of the API's own database.
