import { describe, expect, it } from "vitest";
import { formatLatencyDelta, isTerminal, verdictLabel, verdictTone } from "./format";

describe("formatLatencyDelta", () => {
  it("formats positive, negative, and zero", () => {
    expect(formatLatencyDelta(30)).toBe("+30 ms");
    expect(formatLatencyDelta(-5)).toBe("-5 ms");
    expect(formatLatencyDelta(0)).toBe("±0 ms");
  });
});

describe("verdict helpers", () => {
  it("labels and tones verdicts", () => {
    expect(verdictLabel("PerfRegression")).toBe("Performance regression");
    expect(verdictTone("Pass")).toBe("pass");
    expect(verdictTone("Error")).toBe("fail");
    expect(verdictTone("Unspecified")).toBe("neutral");
  });
});

describe("isTerminal", () => {
  it("is true for finished runs", () => {
    expect(isTerminal("Completed")).toBe(true);
    expect(isTerminal("Failed")).toBe(true);
    expect(isTerminal("Replaying")).toBe(false);
  });
});
