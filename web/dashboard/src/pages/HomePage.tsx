import { Link } from "react-router-dom";
import { AsyncView } from "../components/ui";
import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";

function ProjectList({ orgId }: { orgId: string }) {
  const projects = useAsync(() => api.projects(orgId), [orgId]);
  return (
    <AsyncView state={projects}>
      {(items) =>
        items.length === 0 ? (
          <p className="muted">No projects.</p>
        ) : (
          <ul className="project-list">
            {items.map((project) => (
              <li key={project.id}>
                <Link to={`/orgs/${orgId}/projects/${project.id}/runs`}>{project.name}</Link>
                <span className="muted mono"> {project.gitHubRepo}</span>
              </li>
            ))}
          </ul>
        )
      }
    </AsyncView>
  );
}

export function HomePage() {
  const orgs = useAsync(() => api.organizations(), []);

  return (
    <section>
      <h1>Projects</h1>
      <AsyncView state={orgs}>
        {(items) =>
          items.length === 0 ? (
            <p className="muted">You are not a member of any organization yet.</p>
          ) : (
            items.map((org) => (
              <div key={org.id} className="card">
                <h2>{org.name}</h2>
                <ProjectList orgId={org.id} />
              </div>
            ))
          )
        }
      </AsyncView>
    </section>
  );
}
