export type DiffKind = "added" | "removed" | "changed";

export interface FieldDiff {
  path: string;
  baselineValue: string;
  candidateValue: string;
  kind: DiffKind;
}

export interface ResponseDiff {
  fields: FieldDiff[];
  hasBehavioralChange: boolean;
}

function asRecord(value: unknown): Record<string, unknown> {
  return typeof value === "object" && value !== null ? (value as Record<string, unknown>) : {};
}

function asString(value: unknown): string {
  return typeof value === "string" ? value : "";
}

function toKind(value: unknown): DiffKind {
  const raw = asString(value).toUpperCase();
  if (raw.includes("ADDED")) return "added";
  if (raw.includes("REMOVED")) return "removed";
  return "changed";
}

/**
 * Parse the engine's proto-JSON Diff (as stored in ReplayResult.diffJson) into a
 * structured, render-friendly shape. Tolerates empty / malformed input.
 */
export function parseDiff(diffJson: string): ResponseDiff {
  let raw: unknown;
  try {
    raw = JSON.parse(diffJson && diffJson.trim() !== "" ? diffJson : "{}");
  } catch {
    return { fields: [], hasBehavioralChange: false };
  }

  const record = asRecord(raw);
  const rawFields = Array.isArray(record.fields) ? record.fields : [];
  const fields: FieldDiff[] = rawFields.map((entry) => {
    const field = asRecord(entry);
    return {
      path: asString(field.path),
      baselineValue: asString(field.baselineValue),
      candidateValue: asString(field.candidateValue),
      kind: toKind(field.kind),
    };
  });

  return { fields, hasBehavioralChange: Boolean(record.hasBehavioralChange) };
}
