# Operational Runbook

> Status: Phase 9 · On-call reference for APIDiff.

## Health & where to look

- Each service serves `GET /healthz`. Kubernetes liveness/readiness probes use it.
- Traces: one distributed trace per run, searchable by `apidiff.run_id`.
- Key metric: `apidiff.runs.completed{outcome}` (success / regression / failed).

## Common incidents

### Runs stuck in `Provisioning` / `Replaying`
1. Check the `regression.run` trace for the run id — which step is open?
2. `Replaying` stall → replay engine health + logs; verify candidate env URL
   reachable.
3. Provisioning stall → environment provisioner / cluster capacity.
4. The orchestrator marks a run `Failed` on exception and posts a failing GitHub
   check; a truly stuck run indicates a hang, not an exception — inspect logs.

### Analysis explanations missing
- Analysis is **best-effort**: if the Python service is down, the run still
  completes without explanations (logged as a warning). Check `analysis` health;
  no run fails for this reason.

### Webhook returns 401
- Signature mismatch. Confirm the GitHub webhook secret matches
  `GitHub:WebhookSecret` (from Secret Manager). Rotate via Secret Manager and
  update the GitHub webhook config together.

### GitHub checks not appearing
- When `GitHub:AppId` + `GitHub:PrivateKeyPem` are set, runs post a **Check Run**
  via the App (installation token). Verify the App is installed on the repo and
  the private key is current. Without App creds, a commit **status** is posted if
  `GitHub:Token` is set, otherwise the intended status is only logged.

### Database connection errors
- Verify the `apidiff-db` secret (`connection` key) and Cloud SQL private IP
  reachability; check connection saturation against the SLO alert.

## Deploys & rollback

- Merges to `main` build images and `helm upgrade --wait` to staging
  ([ADR 0011]). A failed health gate auto-runs `helm rollback`.
- Manual rollback: `helm rollback apidiff -n apidiff`.
- Verify: `kubectl -n apidiff rollout status deploy/api`.

## Secret rotation

- **DB password**: rotate in Cloud SQL, update the `db-connection` secret
  version, restart the `api` deployment.
- **GitHub webhook secret / token**: update Secret Manager and the GitHub side
  together; no redeploy needed if mounted as env from the synced secret + pod
  restart.
