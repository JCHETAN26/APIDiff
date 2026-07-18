using ApiDiff.Api.Auth;
using ApiDiff.Api.Features.Organizations;
using ApiDiff.Api.Features.Projects;
using ApiDiff.Api.Features.Scenarios;
using ApiDiff.Api.Health;
using ApiDiff.Api.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApiDiffDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddApiDiffAuth(builder.Configuration);
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAccessControl, AccessControl>();
builder.Services.AddScoped<IAuditService, AuditService>();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

// Apply pending migrations on startup so the service is usable out of the box.
if (!builder.Configuration.GetValue("SkipStartupMigration", false))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<ApiDiffDbContext>().Database.MigrateAsync();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi(); // OpenAPI document at /openapi/v1.json

app.MapGet("/healthz", () => Results.Ok(HealthReport.Current()));

var api = app.MapGroup("/api/v1").RequireAuthorization();
api.MapOrganizationEndpoints();
api.MapProjectEndpoints();
api.MapScenarioEndpoints();

await app.RunAsync();

// Exposed so integration tests can use WebApplicationFactory<Program>.
public partial class Program;
