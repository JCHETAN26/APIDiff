import { getToken } from "./session";
import type { Organization, Project, ReplayResult, Run, RunDetail } from "./types";

const BASE = (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? "http://localhost:8080";

export class ApiError extends Error {
  readonly status: number;
  constructor(message: string, status: number) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}

async function get<T>(path: string): Promise<T> {
  const token = getToken();
  const response = await fetch(`${BASE}/api/v1${path}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });
  if (!response.ok) {
    throw new ApiError(
      response.status === 401 ? "Not authenticated" : `Request failed (${response.status})`,
      response.status,
    );
  }
  return (await response.json()) as T;
}

export const api = {
  organizations: () => get<Organization[]>("/organizations"),
  projects: (orgId: string) => get<Project[]>(`/organizations/${orgId}/projects`),
  runs: (orgId: string, projectId: string) => get<Run[]>(`/organizations/${orgId}/projects/${projectId}/runs`),
  run: (orgId: string, projectId: string, runId: string) =>
    get<RunDetail>(`/organizations/${orgId}/projects/${projectId}/runs/${runId}`),
  results: (orgId: string, projectId: string, runId: string) =>
    get<ReplayResult[]>(`/organizations/${orgId}/projects/${projectId}/runs/${runId}/results`),
};
