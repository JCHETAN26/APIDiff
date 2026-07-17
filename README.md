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

Prerequisites: Docker. (Go 1.26+, Node 22+, Python 3.13+ for running individual
services outside containers; .NET 9 SDK for building `api` locally.)

```bash
docker compose up --build      # bring up Postgres + services
```

## Engineering practice

- No direct pushes to `main`; all changes land via PR from a feature branch.
- Branch names: `feat/…`, `fix/…`, `chore/…`, `docs/…`, `test/…`.
- [Conventional Commits](https://www.conventionalcommits.org/).
- CI (format → lint → test → build → security scan) must be green to merge.

See `docs/` for the architecture and phased build plan (kept local).
