package server

import (
	"crypto/tls"
	"crypto/x509"
	"errors"
	"os"

	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials"
)

// Options returns gRPC server options. When TLS_CERT_FILE and TLS_KEY_FILE
// are set, the server requires TLS; if TLS_CLIENT_CA_FILE is also set, it
// requires and verifies client certificates (mTLS). Otherwise the server is
// insecure (local/dev), matching the plaintext default in ADR 0011's cluster
// where mTLS is provided by the mesh.
func Options() ([]grpc.ServerOption, error) {
	certFile := os.Getenv("TLS_CERT_FILE")
	keyFile := os.Getenv("TLS_KEY_FILE")
	if certFile == "" || keyFile == "" {
		return nil, nil
	}

	cert, err := tls.LoadX509KeyPair(certFile, keyFile)
	if err != nil {
		return nil, err
	}

	cfg := &tls.Config{
		Certificates: []tls.Certificate{cert},
		MinVersion:   tls.VersionTLS13,
	}

	if caFile := os.Getenv("TLS_CLIENT_CA_FILE"); caFile != "" {
		caPem, err := os.ReadFile(caFile)
		if err != nil {
			return nil, err
		}
		pool := x509.NewCertPool()
		if !pool.AppendCertsFromPEM(caPem) {
			return nil, errors.New("failed to parse client CA certificate")
		}
		cfg.ClientCAs = pool
		cfg.ClientAuth = tls.RequireAndVerifyClientCert
	}

	return []grpc.ServerOption{grpc.Creds(credentials.NewTLS(cfg))}, nil
}
