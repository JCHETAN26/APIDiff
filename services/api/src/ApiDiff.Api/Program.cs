using ApiDiff.Api.Health;
using ApiDiff.Api.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApiDiffDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

var app = builder.Build();

// Phase 0/1: liveness only. Auth, projects, webhooks, and orchestration land in
// Phase 2 onward.
app.MapGet("/healthz", () => Results.Ok(HealthReport.Current()));

app.Run();

// Exposed so integration tests can use WebApplicationFactory<Program>.
public partial class Program;
