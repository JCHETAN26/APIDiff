# APIDiff

Safely refactor existing APIs. APIDiff captures **sanitized staging traffic**,
replays it against a **pull-request build**, and detects **behavioral** and
**performance regressions** before deployment.

## Monorepo layout

```
proto/                 # gRPC contracts — source of truth for service boundaries
services/
  api/                 # C# / ASP.NET Core — auth, projects, webhooks, orchestration, REST
  replay-engine/       # Go — high-concurrency request replay + diffing
  analysis/            # Python — cluster duplicate scenarios, explain failures
web/dashboard/         # React + TypeScript — review runs and response diffs
infra/
  terraform/           # GCP provisioning
  deploy/              # Kubernetes / Helm (base + ephemeral per-PR env)
.github/workflows/     # CI/CD
docs/adr/              # Architecture Decision Records
```

## Local development

Prerequisites: Docker, and Node 22+ for the dashboard. (Go 1.25+, Python 3.13+,
.NET 9 SDK only if you build a service outside its container.)

### Try it locally (no GCP, no identity provider)

1. **Backend** — Postgres + the four services:

   ```bash
   docker compose up --build
   ```

   In compose the API runs in **dev-auth mode** (`Authentication:DevMode=true`):
   it trusts the bearer token as the signed-in user, so no OIDC provider is
   needed. This is off by default and never enabled in production.

2. **Dashboard**:

   ```bash
   cd web/dashboard && npm install && npm run dev
   ```

   Open <http://localhost:5173>. Sign in by pasting any word as the token
   (e.g. `demo-user`).

3. **Seed a sample run** so there's something to look at:

   ```bash
   scripts/seed-demo.sh
   ```

   Then browse **Acme Corp → Orders API → PR #128** to see verdicts, the
   response diff viewer, latency deltas, and the analysis shortlist.

> Production auth uses OIDC single sign-on: set `Authentication:Authority`
> (API) and `VITE_OIDC_AUTHORITY` / `VITE_OIDC_CLIENT_ID` (dashboard).

## Engineering practice

- No direct pushes to `main`; all changes land via PR from a feature branch.
- Branch names: `feat/…`, `fix/…`, `chore/…`, `docs/…`, `test/…`.
- [Conventional Commits](https://www.conventionalcommits.org/).
- CI (format → lint → test → build → security scan) must be green to merge.

See `docs/` for the architecture and phased build plan (kept local).
