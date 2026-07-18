# ADR 0002: gRPC, contract-first, for service-to-service communication

- **Status:** Accepted
- **Date:** 2026-07-17

## Context

The C# API orchestrator must call the Go replay engine and the Python analysis
service. These are high-volume, strongly-typed, internal calls across three
languages.

## Decision

Use gRPC for all internal service-to-service communication. The `.proto` files
in `proto/` are the source of truth and are written **before** service
implementations. Stubs for C#, Go, and Python are generated in CI.

## Consequences

- The three services can be built in parallel against a fixed interface.
- Strong typing and streaming across language boundaries; efficient binary
  transport for high-throughput replay results.
- Breaking-change detection via `buf breaking` protects consumers.
- REST (from the dashboard) remains a separate, public-facing surface on the
  API service only.
- Open question deferred to ADR 0005: commit generated stubs vs. regenerate in
  CI.

## Alternatives considered

- **REST/JSON everywhere:** simpler tooling but weaker typing, no native
  streaming, and more boilerplate for internal calls.
