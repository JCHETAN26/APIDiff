/** Health payload returned by every APIDiff backend service. */
export interface HealthStatus {
  service: string;
  status: string;
  version: string;
}

/** True when a service reports itself healthy. */
export function isHealthy(status: HealthStatus): boolean {
  return status.status === "ok";
}
