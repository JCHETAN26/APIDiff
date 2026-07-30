// Command replay-realworld-benchmark measures APIDiff replay throughput against
// a real public API. It captures reference responses, then replays the same
// scenarios sequentially and concurrently through the replay engine.
package main

import (
	"context"
	"encoding/json"
	"flag"
	"fmt"
	"io"
	"log"
	"net/http"
	"os"
	"sort"
	"time"

	commonv1 "github.com/apidiff/replay-engine/gen/apidiff/common/v1"
	replayv1 "github.com/apidiff/replay-engine/gen/apidiff/replay/v1"
	"github.com/apidiff/replay-engine/internal/replay"
)

const hnBaseURL = "https://hacker-news.firebaseio.com"

type report struct {
	API                 string         `json:"api"`
	BaseURL             string         `json:"base_url"`
	ScenarioCount       int            `json:"scenario_count"`
	ConcurrentWorkers   int            `json:"concurrent_workers"`
	RequestTimeoutMs    int64          `json:"request_timeout_ms"`
	LatencyRatio        float64        `json:"latency_regression_ratio"`
	IgnoredFields       []string       `json:"ignored_fields"`
	CaptureElapsedMs    int64          `json:"capture_elapsed_ms"`
	SequentialMs        int64          `json:"sequential_ms"`
	ConcurrentMs        int64          `json:"concurrent_ms"`
	TimeReductionPct    float64        `json:"time_reduction_pct"`
	SequentialScenarios float64        `json:"sequential_scenarios_per_second"`
	ConcurrentScenarios float64        `json:"concurrent_scenarios_per_second"`
	SequentialVerdicts  map[string]int `json:"sequential_verdicts"`
	ConcurrentVerdicts  map[string]int `json:"concurrent_verdicts"`
	HTTPStatusCounts    map[string]int `json:"http_status_counts"`
	ScenarioIDs         []string       `json:"scenario_ids"`
	StartedAt           time.Time      `json:"started_at"`
	CompletedAt         time.Time      `json:"completed_at"`
	Notes               string         `json:"notes"`
}

func main() {
	var (
		count       = flag.Int("count", 200, "number of Hacker News item scenarios to replay")
		concurrent  = flag.Int("concurrency", 16, "worker-pool concurrency for the concurrent replay")
		timeout     = flag.Duration("timeout", 10*time.Second, "per-request timeout")
		latencyRate = flag.Float64("latency-ratio", 1.0, "fraction slower than captured baseline before a latency regression is reported")
		jsonOut     = flag.String("json-out", "", "optional path to write the JSON report")
	)
	flag.Parse()

	if *count <= 0 {
		log.Fatal("-count must be positive")
	}
	if *concurrent <= 1 {
		log.Fatal("-concurrency must be greater than 1")
	}

	ctx := context.Background()
	client := &http.Client{Timeout: *timeout}
	started := time.Now().UTC()

	ids, err := topStoryIDs(ctx, client, *count)
	if err != nil {
		log.Fatalf("top stories: %v", err)
	}
	if len(ids) < *count {
		log.Fatalf("wanted %d scenarios, got %d top-story ids", *count, len(ids))
	}

	captureStart := time.Now()
	scenarios, statusCounts, err := captureScenarios(ctx, client, ids[:*count])
	if err != nil {
		log.Fatalf("capture scenarios: %v", err)
	}
	captureElapsed := time.Since(captureStart)

	ignore := []string{"score", "descendants", "kids"}
	seqDuration, seqVerdicts, err := runReplay(ctx, scenarios, 1, *timeout, *latencyRate, ignore)
	if err != nil {
		log.Fatalf("sequential replay: %v", err)
	}
	conDuration, conVerdicts, err := runReplay(ctx, scenarios, *concurrent, *timeout, *latencyRate, ignore)
	if err != nil {
		log.Fatalf("concurrent replay: %v", err)
	}

	scenarioIDs := make([]string, 0, len(scenarios))
	for _, s := range scenarios {
		scenarioIDs = append(scenarioIDs, s.GetId())
	}
	sort.Strings(scenarioIDs)

	r := report{
		API:                 "Hacker News Firebase API",
		BaseURL:             hnBaseURL,
		ScenarioCount:       len(scenarios),
		ConcurrentWorkers:   *concurrent,
		RequestTimeoutMs:    timeout.Milliseconds(),
		LatencyRatio:        *latencyRate,
		IgnoredFields:       ignore,
		CaptureElapsedMs:    captureElapsed.Milliseconds(),
		SequentialMs:        seqDuration.Milliseconds(),
		ConcurrentMs:        conDuration.Milliseconds(),
		TimeReductionPct:    percentReduction(seqDuration, conDuration),
		SequentialScenarios: throughput(len(scenarios), seqDuration),
		ConcurrentScenarios: throughput(len(scenarios), conDuration),
		SequentialVerdicts:  seqVerdicts,
		ConcurrentVerdicts:  conVerdicts,
		HTTPStatusCounts:    statusCounts,
		ScenarioIDs:         scenarioIDs,
		StartedAt:           started,
		CompletedAt:         time.Now().UTC(),
		Notes:               "References are captured immediately before replay. score, descendants, and kids are ignored because they are volatile on live Hacker News stories.",
	}

	payload, err := json.MarshalIndent(r, "", "  ")
	if err != nil {
		log.Fatalf("marshal report: %v", err)
	}
	fmt.Println(string(payload))
	if *jsonOut != "" {
		if err := os.WriteFile(*jsonOut, append(payload, '\n'), 0o644); err != nil {
			log.Fatalf("write report: %v", err)
		}
	}
}

func topStoryIDs(ctx context.Context, client *http.Client, count int) ([]int, error) {
	req, err := http.NewRequestWithContext(ctx, http.MethodGet, hnBaseURL+"/v0/topstories.json", nil)
	if err != nil {
		return nil, err
	}
	req.Header.Set("User-Agent", "APIDiff real-world benchmark")
	resp, err := client.Do(req)
	if err != nil {
		return nil, err
	}
	defer func() { _ = resp.Body.Close() }()
	if resp.StatusCode != http.StatusOK {
		return nil, fmt.Errorf("topstories status %d", resp.StatusCode)
	}
	var ids []int
	if err := json.NewDecoder(resp.Body).Decode(&ids); err != nil {
		return nil, err
	}
	if len(ids) > count {
		ids = ids[:count]
	}
	return ids, nil
}

func captureScenarios(ctx context.Context, client *http.Client, ids []int) ([]*replayv1.Scenario, map[string]int, error) {
	scenarios := make([]*replayv1.Scenario, 0, len(ids))
	statusCounts := map[string]int{}
	for _, id := range ids {
		path := fmt.Sprintf("/v0/item/%d.json", id)
		body, latency, status, err := fetch(ctx, client, path)
		if err != nil {
			return nil, nil, fmt.Errorf("%s: %w", path, err)
		}
		statusCounts[fmt.Sprintf("%d", status)]++
		scenarios = append(scenarios, &replayv1.Scenario{
			Id: fmt.Sprintf("hn-%d", id),
			Request: &commonv1.HttpRequest{
				Method: http.MethodGet,
				Path:   path,
				Headers: []*commonv1.Header{
					{Name: "User-Agent", Value: "APIDiff real-world benchmark"},
				},
			},
			ReferenceResponse: &commonv1.HttpResponse{
				StatusCode: int32(status),
				Body:       body,
				LatencyMs:  latency.Milliseconds(),
			},
		})
	}
	return scenarios, statusCounts, nil
}

func fetch(ctx context.Context, client *http.Client, path string) ([]byte, time.Duration, int, error) {
	req, err := http.NewRequestWithContext(ctx, http.MethodGet, hnBaseURL+path, nil)
	if err != nil {
		return nil, 0, 0, err
	}
	req.Header.Set("User-Agent", "APIDiff real-world benchmark")
	start := time.Now()
	resp, err := client.Do(req)
	if err != nil {
		return nil, 0, 0, err
	}
	defer func() { _ = resp.Body.Close() }()
	body, err := io.ReadAll(resp.Body)
	if err != nil {
		return nil, 0, 0, err
	}
	return body, time.Since(start), resp.StatusCode, nil
}

func runReplay(ctx context.Context, scenarios []*replayv1.Scenario, concurrency int, timeout time.Duration, latencyRatio float64, ignore []string) (time.Duration, map[string]int, error) {
	req := &replayv1.ReplayRequest{
		RunId:     fmt.Sprintf("hn-realworld-c%d", concurrency),
		Scenarios: scenarios,
		Candidate: &commonv1.Target{
			Label:   "hacker-news",
			BaseUrl: hnBaseURL,
		},
		Config: &replayv1.ReplayConfig{
			Concurrency:            int32(concurrency),
			RequestTimeoutMs:       timeout.Milliseconds(),
			IgnoreFields:           ignore,
			LatencyRegressionRatio: latencyRatio,
		},
	}

	verdicts := map[string]int{}
	start := time.Now()
	err := replay.NewEngine().Run(ctx, req, func(r *replayv1.ReplayResult) error {
		verdicts[r.GetVerdict().String()]++
		return nil
	})
	return time.Since(start), verdicts, err
}

func percentReduction(sequential, concurrent time.Duration) float64 {
	if sequential <= 0 {
		return 0
	}
	return (float64(sequential-concurrent) / float64(sequential)) * 100
}

func throughput(count int, d time.Duration) float64 {
	if d <= 0 {
		return 0
	}
	return float64(count) / d.Seconds()
}
