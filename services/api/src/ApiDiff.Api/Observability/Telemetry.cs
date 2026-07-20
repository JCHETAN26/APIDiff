using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ApiDiff.Api.Observability;

/// <summary>
/// OpenTelemetry wiring for the API service. Tracing and metrics export via OTLP
/// when an endpoint is configured (OTEL_EXPORTER_OTLP_ENDPOINT); otherwise the
/// SDK is registered but exports nothing, so it is a no-op in local/dev.
/// </summary>
public static class Telemetry
{
    public const string ServiceName = "apidiff-api";

    /// <summary>Activity source for spans the orchestrator creates around a run.</summary>
    public static readonly ActivitySource ActivitySource = new(ServiceName);

    /// <summary>Meter for custom run metrics.</summary>
    public static readonly Meter Meter = new(ServiceName);

    /// <summary>Counter of completed runs, tagged by outcome (success/regression/failed).</summary>
    public static readonly Counter<long> RunsCompleted =
        Meter.CreateCounter<long>("apidiff.runs.completed", description: "Regression runs that reached a terminal state.");

    public static IServiceCollection AddApiDiffTelemetry(this IServiceCollection services, IConfiguration config)
    {
        var otlpEndpoint = config["OTEL_EXPORTER_OTLP_ENDPOINT"]
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        var hasExporter = !string.IsNullOrWhiteSpace(otlpEndpoint);

        var resource = ResourceBuilder.CreateDefault().AddService(ServiceName);

        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(resource)
                    .AddSource(ServiceName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddGrpcClientInstrumentation();
                if (hasExporter)
                {
                    tracing.AddOtlpExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resource)
                    .AddMeter(ServiceName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
                if (hasExporter)
                {
                    metrics.AddOtlpExporter();
                }
            });

        return services;
    }
}
