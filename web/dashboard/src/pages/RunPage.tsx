import { useEffect, useRef } from "react";
import { Link, useParams } from "react-router-dom";
import { DiffViewer } from "../components/DiffViewer";
import { AsyncView, StatusBadge, VerdictBadge } from "../components/ui";
import { api } from "../lib/api";
import { formatLatencyDelta, isTerminal } from "../lib/format";
import type { Explanation, ReplayResult, RunDetail } from "../lib/types";
import { useAsync } from "../lib/useAsync";

export function RunPage() {
  const { orgId = "", projectId = "", runId = "" } = useParams();
  const detail = useAsync(() => api.run(orgId, projectId, runId), [orgId, projectId, runId]);
  const results = useAsync(() => api.results(orgId, projectId, runId), [orgId, projectId, runId]);
  const explanations = useAsync(() => api.explanations(orgId, projectId, runId), [orgId, projectId, runId]);

  const terminal = detail.data ? isTerminal(detail.data.run.status) : false;

  // Poll while the run is still in progress, without resetting the interval on
  // every render.
  const reloadRef = useRef<() => void>(() => {});
  reloadRef.current = () => {
    detail.reload();
    results.reload();
    explanations.reload();
  };
  useEffect(() => {
    if (terminal) return;
    const id = setInterval(() => reloadRef.current(), 3000);
    return () => clearInterval(id);
  }, [terminal]);

  return (
    <section>
      <Link to={`/orgs/${orgId}/projects/${projectId}/runs`} className="muted">
        ← All runs
      </Link>
      <AsyncView state={detail}>{(data) => <RunHeader detail={data} />}</AsyncView>
      <AsyncView state={explanations}>{(items) => <Analysis items={items} />}</AsyncView>
      <AsyncView state={results}>{(items) => <Results items={items} />}</AsyncView>
    </section>
  );
}

function Analysis({ items }: { items: Explanation[] }) {
  if (items.length === 0) {
    return null;
  }
  return (
    <div className="card">
      <h2>Analysis</h2>
      {items.map((item) => (
        <div key={item.id} className="explanation">
          <div className="row">
            <h3>{item.title}</h3>
            <span className="badge badge-fail">{Math.round(item.severity * 100)}%</span>
          </div>
          <p className="muted">
            {item.likelyCause} · {item.scenarioIds.length} scenario(s)
          </p>
          {item.detail ? <p>{item.detail}</p> : null}
        </div>
      ))}
    </div>
  );
}

function RunHeader({ detail }: { detail: RunDetail }) {
  const { run, totalResults, regressions } = detail;
  return (
    <div className="card">
      <div className="row">
        <h1>
          PR #{run.pullRequestNumber} <span className="mono muted">{run.commitSha.slice(0, 10)}</span>
        </h1>
        <StatusBadge status={run.status} />
      </div>
      <p className={regressions > 0 ? "error" : "muted"}>
        {totalResults === 0
          ? "No scenarios replayed."
          : regressions > 0
            ? `${regressions} of ${totalResults} scenarios regressed.`
            : `All ${totalResults} scenarios passed.`}
      </p>
    </div>
  );
}

function Results({ items }: { items: ReplayResult[] }) {
  if (items.length === 0) {
    return <p className="muted">No results yet.</p>;
  }

  return (
    <div className="results">
      {items.map((result) => (
        <article key={result.id} className="card result">
          <div className="row">
            <h3 className="mono">
              {result.scenarioMethod} {result.scenarioPath}
            </h3>
            <VerdictBadge verdict={result.verdict} />
          </div>
          <p className="muted latency">
            baseline {result.baselineLatencyMs} ms → candidate {result.candidateLatencyMs} ms (
            <strong>{formatLatencyDelta(result.latencyDeltaMs)}</strong>)
          </p>
          {result.error ? (
            <p className="error mono">{result.error}</p>
          ) : (
            <DiffViewer diffJson={result.diffJson} />
          )}
        </article>
      ))}
    </div>
  );
}
