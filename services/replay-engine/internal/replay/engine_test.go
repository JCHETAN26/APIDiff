package replay

import (
	"context"
	"fmt"
	"net/http"
	"net/http/httptest"
	"runtime"
	"testing"
	"time"

	commonv1 "github.com/apidiff/replay-engine/gen/apidiff/common/v1"
	replayv1 "github.com/apidiff/replay-engine/gen/apidiff/replay/v1"
)

func jsonServer(t *testing.T, body string) *httptest.Server {
	t.Helper()
	srv := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, _ *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		_, _ = w.Write([]byte(body))
	}))
	t.Cleanup(srv.Close)
	return srv
}

func scenario(id string) *replayv1.Scenario {
	return &replayv1.Scenario{Id: id, Request: &commonv1.HttpRequest{Method: "GET", Path: "/"}}
}

func runAll(t *testing.T, req *replayv1.ReplayRequest) []*replayv1.ReplayResult {
	t.Helper()
	var out []*replayv1.ReplayResult
	err := NewEngine().Run(context.Background(), req, func(r *replayv1.ReplayResult) error {
		out = append(out, r)
		return nil
	})
	if err != nil {
		t.Fatalf("Run: %v", err)
	}
	return out
}

func TestBehavioralRegression(t *testing.T) {
	base := jsonServer(t, `{"total":10}`)
	cand := jsonServer(t, `{"total":12}`)

	results := runAll(t, &replayv1.ReplayRequest{
		RunId:     "run-1",
		Baseline:  &commonv1.Target{BaseUrl: base.URL},
		Candidate: &commonv1.Target{BaseUrl: cand.URL},
		Scenarios: []*replayv1.Scenario{scenario("s1")},
	})

	if len(results) != 1 {
		t.Fatalf("want 1 result, got %d", len(results))
	}
	if results[0].GetVerdict() != commonv1.Verdict_VERDICT_BEHAVIORAL_REGRESSION {
		t.Errorf("verdict = %v", results[0].GetVerdict())
	}
	if !results[0].GetDiff().GetHasBehavioralChange() {
		t.Errorf("expected behavioral change")
	}
}

func TestPass(t *testing.T) {
	base := jsonServer(t, `{"ok":true}`)
	cand := jsonServer(t, `{"ok":true}`)

	results := runAll(t, &replayv1.ReplayRequest{
		Baseline:  &commonv1.Target{BaseUrl: base.URL},
		Candidate: &commonv1.Target{BaseUrl: cand.URL},
		Scenarios: []*replayv1.Scenario{scenario("s1")},
	})
	if results[0].GetVerdict() != commonv1.Verdict_VERDICT_PASS {
		t.Errorf("verdict = %v", results[0].GetVerdict())
	}
}

func TestReferenceModeBaseline(t *testing.T) {
	cand := jsonServer(t, `{"ok":true}`)

	// No baseline target: the scenario's reference response is the baseline.
	s := scenario("s1")
	s.ReferenceResponse = &commonv1.HttpResponse{StatusCode: 200, Body: []byte(`{"ok":false}`), LatencyMs: 5}

	results := runAll(t, &replayv1.ReplayRequest{
		Candidate: &commonv1.Target{BaseUrl: cand.URL},
		Scenarios: []*replayv1.Scenario{s},
	})
	if results[0].GetVerdict() != commonv1.Verdict_VERDICT_BEHAVIORAL_REGRESSION {
		t.Errorf("verdict = %v, want behavioral regression", results[0].GetVerdict())
	}
}

func TestPerfRegression(t *testing.T) {
	slow := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, _ *http.Request) {
		time.Sleep(60 * time.Millisecond)
		_, _ = w.Write([]byte(`{"ok":true}`))
	}))
	t.Cleanup(slow.Close)

	s := scenario("s1")
	s.ReferenceResponse = &commonv1.HttpResponse{StatusCode: 200, Body: []byte(`{"ok":true}`), LatencyMs: 5}

	results := runAll(t, &replayv1.ReplayRequest{
		Candidate: &commonv1.Target{BaseUrl: slow.URL},
		Scenarios: []*replayv1.Scenario{s},
		Config:    &replayv1.ReplayConfig{LatencyRegressionRatio: 0.2},
	})
	if results[0].GetVerdict() != commonv1.Verdict_VERDICT_PERF_REGRESSION {
		t.Errorf("verdict = %v, want perf regression (delta=%dms)", results[0].GetVerdict(), results[0].GetLatencyDeltaMs())
	}
}

func TestCandidateError(t *testing.T) {
	results := runAll(t, &replayv1.ReplayRequest{
		Candidate: &commonv1.Target{BaseUrl: "http://127.0.0.1:1"},
		Scenarios: []*replayv1.Scenario{scenario("s1")},
		Config:    &replayv1.ReplayConfig{RequestTimeoutMs: 500},
	})
	if results[0].GetVerdict() != commonv1.Verdict_VERDICT_ERROR {
		t.Errorf("verdict = %v, want error", results[0].GetVerdict())
	}
	if results[0].GetError() == "" {
		t.Errorf("expected error message")
	}
}

func TestConcurrencyProcessesAllWithoutLeak(t *testing.T) {
	base := jsonServer(t, `{"ok":true}`)
	cand := jsonServer(t, `{"ok":true}`)

	const n = 200
	scenarios := make([]*replayv1.Scenario, n)
	for i := range scenarios {
		scenarios[i] = scenario(fmt.Sprintf("s%d", i))
	}
	req := &replayv1.ReplayRequest{
		Baseline:  &commonv1.Target{BaseUrl: base.URL},
		Candidate: &commonv1.Target{BaseUrl: cand.URL},
		Scenarios: scenarios,
		Config:    &replayv1.ReplayConfig{Concurrency: 16, RequestTimeoutMs: 5000},
	}

	engine := NewEngine()
	collect := func() []*replayv1.ReplayResult {
		var out []*replayv1.ReplayResult
		if err := engine.Run(context.Background(), req, func(r *replayv1.ReplayResult) error {
			out = append(out, r)
			return nil
		}); err != nil {
			t.Fatalf("Run: %v", err)
		}
		return out
	}

	// First run warms the connection pool; measure the steady-state goroutine
	// count, then assert a second identical run leaks no worker goroutines.
	if got := collect(); len(got) != n {
		t.Fatalf("first run: want %d results, got %d", n, len(got))
	}
	settle()
	before := runtime.NumGoroutine()

	results := collect()
	if len(results) != n {
		t.Fatalf("second run: want %d results, got %d", n, len(results))
	}
	seen := make(map[string]bool, n)
	for _, r := range results {
		seen[r.GetScenarioId()] = true
	}
	if len(seen) != n {
		t.Errorf("expected %d unique scenarios, got %d", n, len(seen))
	}

	settle()
	if after := runtime.NumGoroutine(); after > before+5 {
		t.Errorf("possible goroutine leak across runs: before=%d after=%d", before, after)
	}
}

func settle() {
	time.Sleep(100 * time.Millisecond)
	runtime.GC()
}
