// Command replay-engine is the high-concurrency request replay service. It
// serves the gRPC ReplayService and an HTTP health endpoint for Kubernetes.
package main

import (
	"context"
	"encoding/json"
	"errors"
	"log"
	"net"
	"net/http"
	"os"
	"os/signal"
	"syscall"

	"go.opentelemetry.io/contrib/instrumentation/google.golang.org/grpc/otelgrpc"
	"google.golang.org/grpc"

	replayv1 "github.com/apidiff/replay-engine/gen/apidiff/replay/v1"
	"github.com/apidiff/replay-engine/internal/health"
	"github.com/apidiff/replay-engine/internal/observability"
	"github.com/apidiff/replay-engine/internal/replay"
	"github.com/apidiff/replay-engine/internal/server"
)

// version is overridden at build time via -ldflags "-X main.version=...".
var version = "dev"

func main() {
	ctx, stop := signal.NotifyContext(context.Background(), syscall.SIGINT, syscall.SIGTERM)
	defer stop()

	if err := run(ctx); err != nil {
		log.Fatalf("replay-engine: %v", err)
	}
}

func run(ctx context.Context) error {
	grpcAddr := ":" + envOr("GRPC_PORT", "9090")
	httpAddr := ":" + envOr("PORT", "8081")

	shutdownTracing, err := observability.Setup(ctx)
	if err != nil {
		return err
	}
	defer func() { _ = shutdownTracing(context.Background()) }()

	tlsOpts, err := server.Options()
	if err != nil {
		return err
	}
	grpcOpts := append(tlsOpts, grpc.StatsHandler(otelgrpc.NewServerHandler()))

	grpcServer := grpc.NewServer(grpcOpts...)
	replayv1.RegisterReplayServiceServer(grpcServer, server.New(replay.NewEngine()))

	grpcListener, err := net.Listen("tcp", grpcAddr)
	if err != nil {
		return err
	}

	httpServer := &http.Server{Addr: httpAddr, Handler: healthMux()}

	errCh := make(chan error, 2)
	go func() {
		log.Printf("replay-engine %s: gRPC on %s", version, grpcAddr)
		errCh <- grpcServer.Serve(grpcListener)
	}()
	go func() {
		log.Printf("replay-engine %s: health on %s", version, httpAddr)
		if err := httpServer.ListenAndServe(); err != nil && !errors.Is(err, http.ErrServerClosed) {
			errCh <- err
		}
	}()

	select {
	case <-ctx.Done():
		log.Print("replay-engine: shutting down")
		grpcServer.GracefulStop()
		_ = httpServer.Shutdown(context.Background())
		return nil
	case err := <-errCh:
		return err
	}
}

func healthMux() *http.ServeMux {
	mux := http.NewServeMux()
	mux.HandleFunc("/healthz", func(w http.ResponseWriter, _ *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		_ = json.NewEncoder(w).Encode(health.Report(version))
	})
	return mux
}

func envOr(key, fallback string) string {
	if v := os.Getenv(key); v != "" {
		return v
	}
	return fallback
}
