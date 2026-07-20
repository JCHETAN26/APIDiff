import { parseDiff } from "../lib/diff";

/** Side-by-side, field-level view of a response diff (from ReplayResult.diffJson). */
export function DiffViewer({ diffJson }: { diffJson: string }) {
  const diff = parseDiff(diffJson);

  if (diff.fields.length === 0) {
    return <p className="muted">No response differences.</p>;
  }

  return (
    <table className="diff-table">
      <thead>
        <tr>
          <th>Field</th>
          <th>Baseline</th>
          <th>Candidate</th>
          <th>Change</th>
        </tr>
      </thead>
      <tbody>
        {diff.fields.map((field) => (
          <tr key={`${field.kind}:${field.path}`} className={`diff-${field.kind}`}>
            <td className="mono">{field.path}</td>
            <td className="mono">{field.baselineValue || "—"}</td>
            <td className="mono">{field.candidateValue || "—"}</td>
            <td>{field.kind}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
