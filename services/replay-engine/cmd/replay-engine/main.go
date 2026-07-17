// Command replay-engine is the high-concurrency request replay service.
//
// Phase 0: a minimal HTTP health endpoint so the service builds, runs, and is
// wired into docker-compose. The gRPC replay server lands in Phase 4.
package main

import (
	"encoding/json"
	"log"
	"net/http"
	"os"

	"github.com/apidiff/replay-engine/internal/health"
)

// version is overridden at build time via -ldflags "-X main.version=...".
var version = "dev"

func main() {
	addr := ":" + envOr("PORT", "8081")

	mux := http.NewServeMux()
	mux.HandleFunc("/healthz", func(w http.ResponseWriter, _ *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		_ = json.NewEncoder(w).Encode(health.Report(version))
	})

	log.Printf("replay-engine %s listening on %s", version, addr)
	if err := http.ListenAndServe(addr, mux); err != nil {
		log.Fatalf("server error: %v", err)
	}
}

func envOr(key, fallback string) string {
	if v := os.Getenv(key); v != "" {
		return v
	}
	return fallback
}
