export interface HealthStatus {
  service: string;
  status: string;
  version: string;
}

export interface Organization {
  id: string;
  name: string;
  slug: string;
  role: number;
  createdAt: string;
}

export interface Project {
  id: string;
  organizationId: string;
  name: string;
  slug: string;
  gitHubRepo: string;
  baselineBaseUrl: string;
  createdAt: string;
}

export type RunStatus =
  | "Pending"
  | "Provisioning"
  | "Replaying"
  | "Analyzing"
  | "Completed"
  | "Failed"
  | "Cancelled";

export interface Run {
  id: string;
  projectId: string;
  pullRequestNumber: number;
  commitSha: string;
  status: RunStatus;
  candidateBaseUrl: string | null;
  createdAt: string;
  startedAt: string | null;
  completedAt: string | null;
}

export interface RunDetail {
  run: Run;
  totalResults: number;
  regressions: number;
}

export type Verdict =
  | "Unspecified"
  | "Pass"
  | "BehavioralRegression"
  | "PerfRegression"
  | "Error";

export interface ReplayResult {
  id: string;
  scenarioId: string;
  scenarioMethod: string;
  scenarioPath: string;
  verdict: Verdict;
  diffJson: string;
  baselineLatencyMs: number;
  candidateLatencyMs: number;
  latencyDeltaMs: number;
  error: string | null;
  createdAt: string;
}
