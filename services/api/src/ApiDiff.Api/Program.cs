using ApiDiff.Api.Health;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Phase 0: liveness only. Auth, projects, webhooks, and orchestration land in
// Phase 2 onward.
app.MapGet("/healthz", () => Results.Ok(HealthReport.Current()));

app.Run();

// Exposed so integration tests can use WebApplicationFactory<Program>.
public partial class Program;
