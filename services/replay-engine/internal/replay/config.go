package replay

import (
	"time"

	replayv1 "github.com/apidiff/replay-engine/gen/apidiff/replay/v1"
)

const (
	defaultConcurrency  = 8
	defaultTimeout      = 30 * time.Second
	defaultLatencyRatio = 0.20
)

type config struct {
	concurrency  int
	timeout      time.Duration
	ignoreFields []string
	latencyRatio float64
}

func normalizeConfig(c *replayv1.ReplayConfig) config {
	cfg := config{
		concurrency:  defaultConcurrency,
		timeout:      defaultTimeout,
		latencyRatio: defaultLatencyRatio,
	}
	if c == nil {
		return cfg
	}
	if c.GetConcurrency() > 0 {
		cfg.concurrency = int(c.GetConcurrency())
	}
	if c.GetRequestTimeoutMs() > 0 {
		cfg.timeout = time.Duration(c.GetRequestTimeoutMs()) * time.Millisecond
	}
	if c.GetLatencyRegressionRatio() > 0 {
		cfg.latencyRatio = c.GetLatencyRegressionRatio()
	}
	cfg.ignoreFields = c.GetIgnoreFields()
	return cfg
}
