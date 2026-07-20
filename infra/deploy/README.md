# infra/deploy

Helm chart (`apidiff/`) deploying the four services (`api`, `replay-engine`,
`analysis`, `dashboard`) as Deployments + Services, with HTTP health probes and
Workload Identity on the API's service account.

## Base deployment

```bash
helm upgrade --install apidiff infra/deploy/apidiff \
  --namespace apidiff --create-namespace \
  --set image.registry=us-central1-docker.pkg.dev/<project>/apidiff \
  --set image.tag=<commit-sha> \
  --set serviceAccount.gcpServiceAccount=apidiff-staging-api@<project>.iam.gserviceaccount.com
```

The API reads its Postgres connection string from the `apidiff-db` secret
(key `connection`), synced from Secret Manager.

## Ephemeral per-PR environment (ADR 0004)

The same chart runs the candidate build in an isolated namespace:

```bash
helm install apidiff-pr-<n> infra/deploy/apidiff \
  --namespace pr-<n> --create-namespace \
  -f infra/deploy/apidiff/values-pr.yaml \
  --set image.registry=<registry> --set image.tag=<pr-sha>
```

CI lints and renders the chart on every change (`.github/workflows/infra.yml`).
