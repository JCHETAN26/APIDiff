package server_test

import (
	"context"
	"errors"
	"io"
	"net"
	"net/http"
	"net/http/httptest"
	"testing"

	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials/insecure"
	"google.golang.org/grpc/test/bufconn"

	commonv1 "github.com/apidiff/replay-engine/gen/apidiff/common/v1"
	replayv1 "github.com/apidiff/replay-engine/gen/apidiff/replay/v1"
	"github.com/apidiff/replay-engine/internal/replay"
	"github.com/apidiff/replay-engine/internal/server"
)

func TestReplayOverGRPC(t *testing.T) {
	cand := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, _ *http.Request) {
		_, _ = w.Write([]byte(`{"total":12}`))
	}))
	defer cand.Close()

	lis := bufconn.Listen(1024 * 1024)
	grpcServer := grpc.NewServer()
	replayv1.RegisterReplayServiceServer(grpcServer, server.New(replay.NewEngine()))
	go func() { _ = grpcServer.Serve(lis) }()
	defer grpcServer.Stop()

	conn, err := grpc.NewClient(
		"passthrough:///bufnet",
		grpc.WithContextDialer(func(ctx context.Context, _ string) (net.Conn, error) { return lis.DialContext(ctx) }),
		grpc.WithTransportCredentials(insecure.NewCredentials()),
	)
	if err != nil {
		t.Fatalf("dial: %v", err)
	}
	defer func() { _ = conn.Close() }()

	client := replayv1.NewReplayServiceClient(conn)

	s := &replayv1.Scenario{
		Id:                "s1",
		Request:           &commonv1.HttpRequest{Method: "GET", Path: "/"},
		ReferenceResponse: &commonv1.HttpResponse{StatusCode: 200, Body: []byte(`{"total":10}`), LatencyMs: 5},
	}
	stream, err := client.Replay(context.Background(), &replayv1.ReplayRequest{
		RunId:     "run-1",
		Candidate: &commonv1.Target{BaseUrl: cand.URL},
		Scenarios: []*replayv1.Scenario{s},
	})
	if err != nil {
		t.Fatalf("replay: %v", err)
	}

	var got []*replayv1.ReplayResult
	for {
		msg, err := stream.Recv()
		if errors.Is(err, io.EOF) {
			break
		}
		if err != nil {
			t.Fatalf("recv: %v", err)
		}
		got = append(got, msg.GetResult())
	}

	if len(got) != 1 {
		t.Fatalf("want 1 result, got %d", len(got))
	}
	if got[0].GetScenarioId() != "s1" {
		t.Errorf("scenario id = %q", got[0].GetScenarioId())
	}
	if got[0].GetVerdict() != commonv1.Verdict_VERDICT_BEHAVIORAL_REGRESSION {
		t.Errorf("verdict = %v", got[0].GetVerdict())
	}
}
