import type { ReactNode } from "react";
import { statusTone, type Tone, verdictLabel, verdictTone } from "../lib/format";
import type { AsyncState } from "../lib/useAsync";
import type { RunStatus, Verdict } from "../lib/types";

export function Badge({ tone, children }: { tone: Tone; children: ReactNode }) {
  return <span className={`badge badge-${tone}`}>{children}</span>;
}

export function VerdictBadge({ verdict }: { verdict: Verdict }) {
  return <Badge tone={verdictTone(verdict)}>{verdictLabel(verdict)}</Badge>;
}

export function StatusBadge({ status }: { status: RunStatus }) {
  return <Badge tone={statusTone(status)}>{status}</Badge>;
}

/** Renders loading / error / empty states around async data. */
export function AsyncView<T>({
  state,
  children,
}: {
  state: AsyncState<T>;
  children: (data: T) => ReactNode;
}): ReactNode {
  if (state.loading && state.data === null) return <p className="muted">Loading…</p>;
  if (state.error) return <p className="error">{state.error.message}</p>;
  if (state.data === null) return null;
  return <>{children(state.data)}</>;
}
