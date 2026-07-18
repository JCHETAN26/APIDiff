// Package replay replays captured scenarios against a baseline and a candidate
// target, diffs the responses, and reports per-scenario verdicts and latency.
package replay

import (
	"bytes"
	"context"
	"io"
	"net/http"
	"sync"
	"time"

	commonv1 "github.com/apidiff/replay-engine/gen/apidiff/common/v1"
	replayv1 "github.com/apidiff/replay-engine/gen/apidiff/replay/v1"
	"github.com/apidiff/replay-engine/internal/diff"
)

// Engine executes replay runs with bounded concurrency.
type Engine struct {
	client *http.Client
}

// NewEngine returns an Engine. Per-request timeouts are applied via context, so
// the shared client has no global timeout.
func NewEngine() *Engine {
	return &Engine{
		client: &http.Client{
			Transport: &http.Transport{
				MaxIdleConns:        100,
				MaxIdleConnsPerHost: 100,
			},
		},
	}
}

// Run replays every scenario in req, invoking emit once per result. emit is
// always called from a single goroutine, so callers may write to a stream
// without additional synchronization. Backpressure flows from emit to the
// workers. Run returns when all scenarios are processed, emit errors, or ctx
// is cancelled.
func (e *Engine) Run(ctx context.Context, req *replayv1.ReplayRequest, emit func(*replayv1.ReplayResult) error) error {
	cfg := normalizeConfig(req.GetConfig())

	runCtx, cancel := context.WithCancel(ctx)
	defer cancel()

	jobs := make(chan *replayv1.Scenario)
	results := make(chan *replayv1.ReplayResult)

	var wg sync.WaitGroup
	for i := 0; i < cfg.concurrency; i++ {
		wg.Add(1)
		go func() {
			defer wg.Done()
			for s := range jobs {
				res := e.replayOne(runCtx, req, s, cfg)
				select {
				case results <- res:
				case <-runCtx.Done():
					return
				}
			}
		}()
	}

	go func() {
		defer close(jobs)
		for _, s := range req.GetScenarios() {
			select {
			case jobs <- s:
			case <-runCtx.Done():
				return
			}
		}
	}()

	go func() {
		wg.Wait()
		close(results)
	}()

	for res := range results {
		if err := emit(res); err != nil {
			cancel()
			for range results { // drain so workers unblock and exit
			}
			return err
		}
	}

	return runCtx.Err()
}

func (e *Engine) replayOne(ctx context.Context, req *replayv1.ReplayRequest, s *replayv1.Scenario, cfg config) *replayv1.ReplayResult {
	result := &replayv1.ReplayResult{
		RunId:      req.GetRunId(),
		ScenarioId: s.GetId(),
	}

	// Baseline: replay the live baseline target if provided, otherwise fall back
	// to the reference response captured with the scenario.
	var baseline *commonv1.HttpResponse
	if req.GetBaseline() != nil && req.GetBaseline().GetBaseUrl() != "" {
		resp, err := e.doRequest(ctx, req.GetBaseline(), s.GetRequest(), cfg.timeout)
		if err != nil {
			result.Verdict = commonv1.Verdict_VERDICT_ERROR
			result.Error = "baseline: " + err.Error()
			return result
		}
		baseline = resp
	} else {
		baseline = s.GetReferenceResponse()
	}

	candidate, err := e.doRequest(ctx, req.GetCandidate(), s.GetRequest(), cfg.timeout)
	if err != nil {
		result.BaselineResponse = baseline
		result.Verdict = commonv1.Verdict_VERDICT_ERROR
		result.Error = "candidate: " + err.Error()
		return result
	}

	d := diff.Compare(baseline, candidate, cfg.ignoreFields)

	result.BaselineResponse = baseline
	result.CandidateResponse = candidate
	result.Diff = d
	result.LatencyDeltaMs = candidate.GetLatencyMs() - baseline.GetLatencyMs()
	result.Verdict = decideVerdict(d, baseline, candidate, cfg.latencyRatio)
	return result
}

func (e *Engine) doRequest(ctx context.Context, target *commonv1.Target, req *commonv1.HttpRequest, timeout time.Duration) (*commonv1.HttpResponse, error) {
	url := trimTrailingSlash(target.GetBaseUrl()) + req.GetPath()
	if q := req.GetQuery(); q != "" {
		url += "?" + q
	}

	reqCtx, cancel := context.WithTimeout(ctx, timeout)
	defer cancel()

	httpReq, err := http.NewRequestWithContext(reqCtx, req.GetMethod(), url, bytes.NewReader(req.GetBody()))
	if err != nil {
		return nil, err
	}
	for _, h := range req.GetHeaders() {
		httpReq.Header.Add(h.GetName(), h.GetValue())
	}

	start := time.Now()
	resp, err := e.client.Do(httpReq)
	if err != nil {
		return nil, err
	}
	defer func() { _ = resp.Body.Close() }()

	body, err := io.ReadAll(resp.Body)
	latency := time.Since(start).Milliseconds()
	if err != nil {
		return nil, err
	}

	return &commonv1.HttpResponse{
		StatusCode: int32(resp.StatusCode),
		Headers:    convertHeaders(resp.Header),
		Body:       body,
		LatencyMs:  latency,
	}, nil
}

func decideVerdict(d *replayv1.Diff, baseline, candidate *commonv1.HttpResponse, ratio float64) commonv1.Verdict {
	if d.GetHasBehavioralChange() {
		return commonv1.Verdict_VERDICT_BEHAVIORAL_REGRESSION
	}
	if baseline.GetLatencyMs() > 0 &&
		float64(candidate.GetLatencyMs()) > float64(baseline.GetLatencyMs())*(1+ratio) {
		return commonv1.Verdict_VERDICT_PERF_REGRESSION
	}
	return commonv1.Verdict_VERDICT_PASS
}

func convertHeaders(h http.Header) []*commonv1.Header {
	var headers []*commonv1.Header
	for name, values := range h {
		for _, v := range values {
			headers = append(headers, &commonv1.Header{Name: name, Value: v})
		}
	}
	return headers
}

func trimTrailingSlash(s string) string {
	for len(s) > 0 && s[len(s)-1] == '/' {
		s = s[:len(s)-1]
	}
	return s
}
