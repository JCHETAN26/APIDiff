import type { RunStatus, Verdict } from "./types";

export type Tone = "pass" | "fail" | "neutral";

/** Human-readable latency delta, e.g. "+30 ms", "-5 ms", "±0 ms". */
export function formatLatencyDelta(ms: number): string {
  if (ms === 0) return "±0 ms";
  return `${ms > 0 ? "+" : "-"}${Math.abs(ms)} ms`;
}

const VERDICT_LABELS: Record<Verdict, string> = {
  Unspecified: "Unknown",
  Pass: "Pass",
  BehavioralRegression: "Behavioral regression",
  PerfRegression: "Performance regression",
  Error: "Error",
};

export function verdictLabel(verdict: Verdict): string {
  return VERDICT_LABELS[verdict] ?? verdict;
}

export function verdictTone(verdict: Verdict): Tone {
  switch (verdict) {
    case "Pass":
      return "pass";
    case "BehavioralRegression":
    case "PerfRegression":
    case "Error":
      return "fail";
    default:
      return "neutral";
  }
}

const TERMINAL: ReadonlySet<RunStatus> = new Set<RunStatus>(["Completed", "Failed", "Cancelled"]);

export function isTerminal(status: RunStatus): boolean {
  return TERMINAL.has(status);
}

export function statusTone(status: RunStatus): Tone {
  if (status === "Completed") return "pass";
  if (status === "Failed") return "fail";
  return "neutral";
}
