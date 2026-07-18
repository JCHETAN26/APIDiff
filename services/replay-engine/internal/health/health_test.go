package health

import "testing"

func TestReport(t *testing.T) {
	got := Report("v0.0.0")

	if got.Service != "replay-engine" {
		t.Errorf("Service = %q, want %q", got.Service, "replay-engine")
	}
	if got.Status != "ok" {
		t.Errorf("Status = %q, want %q", got.Status, "ok")
	}
	if got.Version != "v0.0.0" {
		t.Errorf("Version = %q, want %q", got.Version, "v0.0.0")
	}
}
