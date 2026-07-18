// Package server adapts the replay engine to the gRPC ReplayService contract.
package server

import (
	"google.golang.org/grpc"

	replayv1 "github.com/apidiff/replay-engine/gen/apidiff/replay/v1"
	"github.com/apidiff/replay-engine/internal/replay"
)

// ReplayServer implements replayv1.ReplayServiceServer.
type ReplayServer struct {
	replayv1.UnimplementedReplayServiceServer
	engine *replay.Engine
}

// New returns a ReplayServer backed by the given engine.
func New(engine *replay.Engine) *ReplayServer {
	return &ReplayServer{engine: engine}
}

// Replay streams a result for each scenario as it completes.
func (s *ReplayServer) Replay(req *replayv1.ReplayRequest, stream grpc.ServerStreamingServer[replayv1.ReplayResponse]) error {
	return s.engine.Run(stream.Context(), req, func(res *replayv1.ReplayResult) error {
		return stream.Send(&replayv1.ReplayResponse{Result: res})
	})
}
