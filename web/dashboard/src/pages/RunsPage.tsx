import { Link, useParams } from "react-router-dom";
import { AsyncView, StatusBadge } from "../components/ui";
import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";

export function RunsPage() {
  const { orgId = "", projectId = "" } = useParams();
  const runs = useAsync(() => api.runs(orgId, projectId), [orgId, projectId]);

  return (
    <section>
      <div className="row">
        <h1>Runs</h1>
        <button type="button" className="ghost" onClick={() => runs.reload()}>
          Refresh
        </button>
      </div>
      <AsyncView state={runs}>
        {(items) =>
          items.length === 0 ? (
            <p className="muted">No runs yet. Open a pull request to trigger one.</p>
          ) : (
            <table className="list-table">
              <thead>
                <tr>
                  <th>PR</th>
                  <th>Commit</th>
                  <th>Status</th>
                  <th>Created</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {items.map((run) => (
                  <tr key={run.id}>
                    <td>#{run.pullRequestNumber}</td>
                    <td className="mono">{run.commitSha.slice(0, 10)}</td>
                    <td>
                      <StatusBadge status={run.status} />
                    </td>
                    <td>{new Date(run.createdAt).toLocaleString()}</td>
                    <td>
                      <Link to={`/orgs/${orgId}/projects/${projectId}/runs/${run.id}`}>View</Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )
        }
      </AsyncView>
    </section>
  );
}
