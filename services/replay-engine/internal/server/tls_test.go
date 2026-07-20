package server

import (
	"os"
	"testing"
)

func TestServerOptions_InsecureWhenUnset(t *testing.T) {
	t.Setenv("TLS_CERT_FILE", "")
	t.Setenv("TLS_KEY_FILE", "")
	opts, err := Options()
	if err != nil {
		t.Fatalf("Options: %v", err)
	}
	if opts != nil {
		t.Errorf("expected no options when TLS is unset, got %d", len(opts))
	}
}

func TestServerOptions_TLSFromEnv(t *testing.T) {
	certFile, keyFile := writeSelfSignedCert(t)
	t.Setenv("TLS_CERT_FILE", certFile)
	t.Setenv("TLS_KEY_FILE", keyFile)

	opts, err := Options()
	if err != nil {
		t.Fatalf("Options: %v", err)
	}
	if len(opts) != 1 {
		t.Fatalf("expected 1 TLS option, got %d", len(opts))
	}
}

func TestServerOptions_BadCertErrors(t *testing.T) {
	bad := filepath(t, "bad.pem", "not a cert")
	t.Setenv("TLS_CERT_FILE", bad)
	t.Setenv("TLS_KEY_FILE", bad)
	if _, err := Options(); err == nil {
		t.Error("expected error for malformed cert")
	}
}

func filepath(t *testing.T, name, content string) string {
	t.Helper()
	p := t.TempDir() + "/" + name
	if err := os.WriteFile(p, []byte(content), 0o600); err != nil {
		t.Fatal(err)
	}
	return p
}
