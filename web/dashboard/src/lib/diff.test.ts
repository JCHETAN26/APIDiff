import { describe, expect, it } from "vitest";
import { parseDiff } from "./diff";

describe("parseDiff", () => {
  it("parses proto-json fields and kinds", () => {
    const json = JSON.stringify({
      hasBehavioralChange: true,
      fields: [
        { path: "total", baselineValue: "10", candidateValue: "12", kind: "DIFF_KIND_CHANGED" },
        { path: "coupon", baselineValue: "X", kind: "DIFF_KIND_REMOVED" },
        { path: "banner", candidateValue: "Y", kind: "DIFF_KIND_ADDED" },
      ],
    });
    const diff = parseDiff(json);
    expect(diff.hasBehavioralChange).toBe(true);
    expect(diff.fields).toHaveLength(3);
    expect(diff.fields[0]).toEqual({
      path: "total",
      baselineValue: "10",
      candidateValue: "12",
      kind: "changed",
    });
    expect(diff.fields[1].kind).toBe("removed");
    expect(diff.fields[2].kind).toBe("added");
  });

  it("returns empty for blank or malformed input", () => {
    expect(parseDiff("")).toEqual({ fields: [], hasBehavioralChange: false });
    expect(parseDiff("{not json")).toEqual({ fields: [], hasBehavioralChange: false });
    expect(parseDiff("{}").fields).toEqual([]);
  });
});
