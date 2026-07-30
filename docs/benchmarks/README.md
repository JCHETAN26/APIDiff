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

