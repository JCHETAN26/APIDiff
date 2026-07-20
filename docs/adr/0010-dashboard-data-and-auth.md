# ADR 0010: Dashboard data fetching and authentication

- **Status:** Accepted
- **Date:** 2026-07-19

## Context

The React dashboard is the human review surface: it lists runs and shows
per-scenario diffs and latency. It must authenticate to the REST API and reflect
runs that are still in progress.

## Decision

- **Data layer.** A thin typed `fetch` client (`lib/api.ts`) targets the REST
  API under `/api/v1`; a small `useAsync` hook handles loading/error/reload.
  Diffs are parsed from the engine's proto-JSON (`ReplayResult.diffJson`) into a
  render-friendly shape in `lib/diff.ts`.
- **Live updates via polling.** The run detail page polls every 3s while the run
  is non-terminal, stopping once it reaches `Completed`/`Failed`/`Cancelled`. No
  server-sent-events endpoint is required.
- **Auth.** The client sends a bearer token (from `localStorage`) on every
  request. The current sign-in screen accepts a pasted token for development;
  production wires OIDC single sign-on (the API already validates JWTs from a
  configured authority, ADR from Phase 2). The token seam is isolated in
  `lib/session.ts` so swapping in an OIDC flow is local.

## Consequences

- The dashboard is a static SPA with no backend of its own; it only needs the
  REST API URL (`VITE_API_BASE_URL`).
- Polling is simple and good enough for run durations; if runs get long or
  numerous, an SSE/WebSocket stream can replace the poll behind `useAsync`.
- The pasted-token sign-in is a development affordance, not the production auth;
  it must be replaced by OIDC before GA.

## Alternatives considered

- **Server-sent events for live status:** deferred — adds a backend streaming
  endpoint for little gain at current run durations.
- **A data library (React Query):** unnecessary for the handful of read-only
  endpoints; the `useAsync` hook keeps dependencies minimal.
