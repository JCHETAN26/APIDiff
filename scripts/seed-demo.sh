#!/usr/bin/env bash
# Seed a demo organization, project, and one completed regression run so the
# dashboard has something to show. Requires the API running with DevMode auth
# (docker compose up) and a reachable Postgres.
#
# Usage: scripts/seed-demo.sh
#   API_BASE     (default http://localhost:8080)
#   DASHBOARD_BASE (default http://localhost:5173)
#   PGHOST/PGPORT (default localhost / 5432), PGUSER/PGPASSWORD/PGDATABASE (apidiff)
set -euo pipefail

API_BASE="${API_BASE:-http://localhost:8080}"
DASHBOARD_BASE="${DASHBOARD_BASE:-http://localhost:5173}"
TOKEN="${DEMO_TOKEN:-demo-user}"
PGHOST="${PGHOST:-localhost}"; PGPORT="${PGPORT:-5432}"
PGUSER="${PGUSER:-apidiff}"; PGPASSWORD="${PGPASSWORD:-apidiff}"; PGDATABASE="${PGDATABASE:-apidiff}"
export PGPASSWORD

api() { curl -sf -H "Authorization: Bearer ${TOKEN}" -H "Content-Type: application/json" "$@"; }
jqid() { python3 -c "import sys,json;print(json.load(sys.stdin)['id'])"; }
id_by_slug() {
  python3 -c 'import json,sys; slug=sys.argv[1]; print(next((x["id"] for x in json.load(sys.stdin) if x.get("slug") == slug), ""))' "$1"
}
psql_exec() {
  if command -v psql >/dev/null 2>&1; then
    psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDATABASE" -v ON_ERROR_STOP=1
  elif command -v docker >/dev/null 2>&1 && docker compose ps postgres >/dev/null 2>&1; then
    docker compose exec -T postgres psql -U "$PGUSER" -d "$PGDATABASE" -v ON_ERROR_STOP=1
  else
    echo "psql is not installed, and docker compose postgres is not reachable." >&2
    echo "Install psql or run docker compose up --build first." >&2
    return 1
  fi
}

echo "Checking API at ${API_BASE} ..."
api "${API_BASE}/api/v1/organizations" >/dev/null

echo "Creating or reusing organization + project via ${API_BASE} ..."
ORG_ID=$(api "${API_BASE}/api/v1/organizations" | id_by_slug acme)
if [[ -z "$ORG_ID" ]]; then
  ORG_ID=$(api -X POST "${API_BASE}/api/v1/organizations" \
    -d '{"name":"Acme Corp","slug":"acme"}' | jqid)
fi

PROJ_ID=$(api "${API_BASE}/api/v1/organizations/${ORG_ID}/projects" | id_by_slug orders)
if [[ -z "$PROJ_ID" ]]; then
  PROJ_ID=$(api -X POST "${API_BASE}/api/v1/organizations/${ORG_ID}/projects" \
    -d '{"name":"Orders API","slug":"orders","gitHubRepo":"acme/orders","baselineBaseUrl":"https://baseline.acme.test"}' | jqid)
fi

echo "Replacing demo data for PR #128 directly in Postgres ..."
psql_exec <<SQL
DO \$\$
DECLARE
  proj uuid := '${PROJ_ID}';
  run uuid := gen_random_uuid();
  s1 uuid := gen_random_uuid();
  s2 uuid := gen_random_uuid();
  s3 uuid := gen_random_uuid();
  ts timestamptz := now();
BEGIN
  DELETE FROM "RegressionRuns"
    WHERE "ProjectId" = proj AND "PullRequestNumber" = 128;

  DELETE FROM "Scenarios"
    WHERE "ProjectId" = proj AND "Fingerprint" IN ('demo-orders-1001', 'demo-orders-total', 'demo-checkout');

  INSERT INTO "Scenarios"("Id","ProjectId","Method","Path","Query","RequestHeadersJson","RequestBody","ReferenceStatusCode","ReferenceHeadersJson","ReferenceBody","Fingerprint","CapturedAt") VALUES
    (s1, proj, 'GET',  '/v1/orders/1001',       '', '{"accept":"application/json"}', ''::bytea, 200, '{"content-type":"application/json"}', '{"order":{"id":"1001","status":"paid","total":"84.50","currency":"USD"}}'::bytea, 'demo-orders-1001', ts),
    (s2, proj, 'GET',  '/v1/orders/1002/total', '', '{"accept":"application/json"}', ''::bytea, 200, '{"content-type":"application/json"}', '{"order":{"id":"1002","total":"129.00","currency":"USD"}}'::bytea, 'demo-orders-total', ts),
    (s3, proj, 'POST', '/v1/checkout',          '', '{"accept":"application/json"}', '{"cartId":"cart_demo_42","paymentToken":"[REDACTED]"}'::bytea, 200, '{"content-type":"application/json"}', '{"checkout":{"status":"accepted","id":"chk_123"}}'::bytea, 'demo-checkout', ts);

  INSERT INTO "RegressionRuns"("Id","ProjectId","PullRequestNumber","CommitSha","Status","CandidateBaseUrl","CreatedAt","StartedAt","CompletedAt")
    VALUES (run, proj, 128, '9f3c1ab2e4', 'Completed', 'http://candidate.apidiff-pr-128.svc.cluster.local:8080', ts, ts, ts);

  INSERT INTO "ReplayResults"("Id","RunId","ScenarioId","Verdict","DiffJson","BaselineLatencyMs","CandidateLatencyMs","LatencyDeltaMs","Error","CreatedAt") VALUES
    (gen_random_uuid(), run, s1, 'Pass', '{}'::jsonb, 42, 40, -2, NULL, ts),
    (gen_random_uuid(), run, s2, 'BehavioralRegression',
      '{"fields":[{"path":"order.total","baselineValue":"129.00","candidateValue":"12900","kind":"DIFF_KIND_CHANGED"},{"path":"order.currency","baselineValue":"USD","candidateValue":"","kind":"DIFF_KIND_REMOVED"}],"hasBehavioralChange":true}'::jsonb,
      55, 61, 6, NULL, ts),
    (gen_random_uuid(), run, s3, 'PerfRegression', '{}'::jsonb, 80, 240, 160, NULL, ts);

  INSERT INTO "RunExplanations"("Id","RunId","Title","Detail","ScenarioIdsJson","Severity","LikelyCause","CreatedAt") VALUES
    (gen_random_uuid(), run, 'Field \`order.total\` changed in 1 scenario',
      'Candidate returns 12900 where baseline returned 129.00 — looks like a cents-vs-dollars unit change. \`order.currency\` was also removed.',
      ('["'||s2||'"]')::jsonb, 0.82, 'changed field', ts),
    (gen_random_uuid(), run, 'Latency regressed in 1 scenario',
      'POST /v1/checkout is ~160 ms slower on the candidate (80 ms -> 240 ms).',
      ('["'||s3||'"]')::jsonb, 0.55, 'performance regression', ts);
END \$\$;
SQL

echo "Done. Open the dashboard, sign in with any token (e.g. '${TOKEN}'),"
echo "then Acme Corp -> Orders API -> PR #128."
echo "Dashboard: ${DASHBOARD_BASE}/orgs/${ORG_ID}/projects/${PROJ_ID}/runs"
echo "org=${ORG_ID} project=${PROJ_ID}"
