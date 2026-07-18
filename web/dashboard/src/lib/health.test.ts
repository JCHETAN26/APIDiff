import { describe, expect, it } from "vitest";
import { type HealthStatus, isHealthy } from "./health";

describe("isHealthy", () => {
  it("is true when status is ok", () => {
    const status: HealthStatus = { service: "api", status: "ok", version: "0.0.0" };
    expect(isHealthy(status)).toBe(true);
  });

  it("is false otherwise", () => {
    const status: HealthStatus = { service: "api", status: "down", version: "0.0.0" };
    expect(isHealthy(status)).toBe(false);
  });
});
