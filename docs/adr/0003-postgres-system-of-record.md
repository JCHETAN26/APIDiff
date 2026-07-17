# ADR 0003: PostgreSQL as the single system of record

- **Status:** Accepted
- **Date:** 2026-07-17

## Context

APIDiff stores users, projects, test cases (scenarios), regression runs,
results, and audit logs. These are relational, transactional, and queried by
the dashboard.

## Decision

Use a single PostgreSQL database as the system of record, owned exclusively by
the C# API service. Other services receive data via gRPC from the API, not by
direct database access.

## Consequences

- One service owns the schema and migrations (EF Core), avoiding shared-database
  coupling.
- Strong consistency and transactions for audit logging and run state.
- Go and Python stay stateless, simplifying their scaling and the ephemeral
  per-PR environments.
- Hosted on Cloud SQL in GCP (Phase 8).

## Alternatives considered

- **Per-service databases:** more isolation but forces distributed transactions
  and data duplication for a domain that is naturally one relational model.
- **Direct DB access from Go/Python:** rejected — it couples every service to
  the schema and undermines the API's ownership and audit guarantees.
