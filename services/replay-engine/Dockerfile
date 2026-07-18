# syntax=docker/dockerfile:1
FROM golang:1.26-alpine AS build
WORKDIR /src
COPY go.mod ./
RUN go mod download
COPY . .
RUN CGO_ENABLED=0 go build -trimpath -o /out/replay-engine ./cmd/replay-engine

FROM gcr.io/distroless/static-debian12:nonroot
COPY --from=build /out/replay-engine /replay-engine
EXPOSE 8081 9090
ENTRYPOINT ["/replay-engine"]
