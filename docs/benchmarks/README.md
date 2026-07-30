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

## Cross-API Benchmarks

The same benchmark harness was also run against two more public APIs from
Codespaces on 2026-07-30. Each trial captured live reference responses first,
then replayed the same captured scenario set sequentially and with 16 Go
workers.

| API | Scenarios/trial | Trials | Median time reduction | Mean time reduction | Median concurrent throughput | Notes |
|---|---:|---:|---:|---:|---:|---|
| Hacker News Firebase API | 200 | 10 | 93.31% | 92.58% | 319.60 scenarios/s | Live story API with volatile fields ignored |
| JSONPlaceholder API | 200 | 5 | 92.74% | 92.90% | 324.88 scenarios/s | Stable small JSON comment payloads |
| PokeAPI | 100 | 5 | 82.37% | 82.64% | 99.21 scenarios/s | Heavier nested JSON payloads |

JSONPlaceholder trials:

| Trial | Sequential | Concurrent | Time reduction | Concurrent throughput |
|---:|---:|---:|---:|---:|
| 1 | 9.546 s | 0.615 s | 93.55% | 324.88 scenarios/s |
| 2 | 8.458 s | 0.639 s | 92.44% | 312.78 scenarios/s |
| 3 | 8.895 s | 0.614 s | 93.09% | 325.41 scenarios/s |
| 4 | 8.493 s | 0.616 s | 92.74% | 324.55 scenarios/s |
| 5 | 8.395 s | 0.613 s | 92.69% | 325.95 scenarios/s |

PokeAPI trials:

| Trial | Sequential | Concurrent | Time reduction | Concurrent throughput |
|---:|---:|---:|---:|---:|
| 1 | 6.158 s | 1.007 s | 83.63% | 99.21 scenarios/s |
| 2 | 6.115 s | 1.114 s | 81.77% | 89.70 scenarios/s |
| 3 | 5.559 s | 0.989 s | 82.20% | 101.02 scenarios/s |
| 4 | 5.873 s | 1.035 s | 82.37% | 96.55 scenarios/s |
| 5 | 5.686 s | 0.954 s | 83.22% | 104.78 scenarios/s |

Resume-ready wording:

> Benchmarked APIDiff across 20 real-world public API trials
> (Hacker News, JSONPlaceholder, and PokeAPI), reducing replay time by
> 82%-93% median depending on payload size, with about 320 scenarios/s median
> throughput on 200-scenario JSON APIs.

## Newman Comparison

APIDiff can export the same benchmark scenario set as a Postman collection and
time a Newman CLI run against it. This compares APIDiff's 16-worker replay
against Newman's sequential collection execution on the same 200
JSONPlaceholder requests.

Command:

```bash
cd services/replay-engine
go run ./cmd/replay-realworld-benchmark \
  -api jsonplaceholder \
  -count 200 \
  -concurrency 16 \
  -timeout 10s \
  -postman-out ../../docs/benchmarks/newman-jsonplaceholder-trials/postman-trial-1.json \
  -newman-json-out ../../docs/benchmarks/newman-jsonplaceholder-trials/newman-trial-1.json \
  -json-out ../../docs/benchmarks/newman-jsonplaceholder-trials/apidiff-trial-1.json \
  -newman-command "npx -y newman"
```

Result from three Codespaces trials on 2026-07-30:

| Metric | Min | Median | Mean | Max |
|---|---:|---:|---:|---:|
| APIDiff sequential replay | 8.568 s | 8.715 s | 8.848 s | 9.261 s |
| APIDiff concurrent replay | 0.635 s | 0.660 s | 0.656 s | 0.672 s |
| Newman CLI wall-clock | 12.857 s | 13.330 s | 17.088 s | 25.077 s |
| Newman internal collection duration | 11.006 s | 11.419 s | 11.574 s | 12.296 s |
| APIDiff reduction vs Newman wall-clock | 95.05% | 95.05% | 95.81% | 97.32% |
| APIDiff reduction vs Newman internal duration | 94.22% | 94.23% | 94.33% | 94.53% |

Per-trial results:

| Trial | APIDiff concurrent | Newman wall-clock | Newman internal duration | APIDiff vs Newman wall-clock | Newman requests |
|---:|---:|---:|---:|---:|---:|
| 1 | 0.672 s | 25.077 s | 12.296 s | 97.32% | 200/200 passed |
| 2 | 0.660 s | 13.330 s | 11.419 s | 95.05% | 200/200 passed |
| 3 | 0.635 s | 12.857 s | 11.006 s | 95.05% | 200/200 passed |

Notes:

- APIDiff replay timings exclude reference capture, matching the earlier
  sequential vs concurrent benchmark methodology.
- Newman wall-clock includes process startup and `npx` command overhead. The
  Newman internal duration is taken from the Newman JSON reporter's run
  timestamps.
- Newman executed the generated Postman collection sequentially with no failed
  requests in all three trials.

Resume-ready wording:

> Compared APIDiff against Newman CLI on 200 public API requests, reducing
> replay wall-clock by 95.05% median with a 16-worker Go pool while validating
> response and latency regressions.
