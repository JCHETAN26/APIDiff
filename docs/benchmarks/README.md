# Benchmarks

## Real-World API Replay

APIDiff includes a reproducible benchmark against the public Hacker News
Firebase API. The harness captures live reference responses for current top
stories, then replays the same scenarios sequentially and with the Go worker
pool.

Command:

```bash
cd services/replay-engine
go run ./cmd/replay-realworld-benchmark \
  -count 200 \
  -concurrency 16 \
  -timeout 10s \
  -json-out ../../docs/benchmarks/hackernews-replay-200.json
```

Result from the Codespaces run on 2026-07-30:

| Metric | Value |
|---|---:|
| Public API | Hacker News Firebase API |
| Scenarios | 200 |
| Captured HTTP status | 200/200 returned HTTP 200 |
| Sequential replay | 9.806 s |
| Concurrent replay | 0.609 s |
| Time reduction | 93.79% |
| Sequential throughput | 20.39 scenarios/s |
| Concurrent throughput | 328.21 scenarios/s |
| Worker pool size | 16 |

## 10-Trial Real-World Distribution

To check whether the single run was representative, APIDiff was run against the
same public API benchmark 10 more times from Codespaces, using 200 live
Hacker News item scenarios per trial and 16 concurrent workers.

| Metric | Min | Median | Mean | Max |
|---|---:|---:|---:|---:|
| Sequential replay | 8.726 s | 9.472 s | 9.527 s | 10.360 s |
| Concurrent replay | 0.616 s | 0.625 s | 0.701 s | 1.299 s |
| Time reduction | 85.53% | 93.31% | 92.58% | 94.03% |
| Concurrent throughput | 153.91 scenarios/s | 319.60 scenarios/s | 299.11 scenarios/s | 324.17 scenarios/s |

Per-trial results:

| Trial | Sequential | Concurrent | Time reduction | Concurrent throughput |
|---:|---:|---:|---:|---:|
| 1 | 10.360 s | 0.691 s | 93.33% | 289.35 scenarios/s |
| 2 | 9.838 s | 0.624 s | 93.66% | 320.37 scenarios/s |
| 3 | 9.690 s | 0.626 s | 93.53% | 319.16 scenarios/s |
| 4 | 10.332 s | 0.616 s | 94.03% | 324.17 scenarios/s |
| 5 | 9.076 s | 0.623 s | 93.13% | 320.96 scenarios/s |
| 6 | 9.317 s | 0.624 s | 93.29% | 320.04 scenarios/s |
| 7 | 8.982 s | 1.299 s | 85.53% | 153.91 scenarios/s |
| 8 | 9.567 s | 0.619 s | 93.53% | 322.99 scenarios/s |
| 9 | 9.377 s | 0.664 s | 92.92% | 301.07 scenarios/s |
| 10 | 8.726 s | 0.626 s | 92.82% | 319.08 scenarios/s |

Nine of ten trials landed between 92.82% and 94.03% time reduction. Trial 7
shows the expected variance of benchmarking over a public internet API and is
included rather than discarded.

Notes:

- The benchmark uses captured reference responses and replays live candidate
  requests against the same public API.
- `score`, `descendants`, and `kids` are ignored because they are volatile on
  active Hacker News stories.
- The latency regression threshold is `1.0`, meaning the candidate must be more
  than 100% slower than the captured reference latency before APIDiff reports a
  performance regression.
- Treat this as a real-world public API benchmark, not production customer
  traffic.
