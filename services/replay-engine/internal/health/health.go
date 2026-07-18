// Package health provides liveness/readiness reporting for the replay engine.
package health

// Status is the service health state reported over HTTP and (later) gRPC.
type Status struct {
	Service string `json:"service"`
	Status  string `json:"status"`
	Version string `json:"version"`
}

// Report returns the current health status for the given version.
func Report(version string) Status {
	return Status{
		Service: "replay-engine",
		Status:  "ok",
		Version: version,
	}
}
