using ApiDiff.Api.Auth;
using ApiDiff.Api.Capture;
using ApiDiff.Api.Features.Capture;
using ApiDiff.Api.Features.Organizations;
using ApiDiff.Api.Features.Projects;
using ApiDiff.Api.Features.Runs;
using ApiDiff.Api.Features.Scenarios;
using ApiDiff.Api.Health;
using ApiDiff.Api.Observability;
using ApiDiff.Api.Orchestration;
using ApiDiff.Api.Orchestration.GitHub;
using ApiDiff.Api.Orchestration.Kubernetes;
using ApiDiff.Api.Persistence;
using k8s;
using ApiDiff.Api.Webhooks;
using ApiDiff.Contracts.Analysis.V1;
using ApiDiff.Contracts.Replay.V1;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApiDiffDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddApiDiffTelemetry(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddApiDiffAuth(builder.Configuration);
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAccessControl, AccessControl>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddSingleton(SanitizationOptions.Default);
builder.Services.AddSingleton<ISanitizer, Sanitizer>();

// Regression-run orchestration.
builder.Services.Configure<GitHubOptions>(builder.Configuration.GetSection("GitHub"));
builder.Services.Configure<OrchestrationOptions>(builder.Configuration.GetSection("Orchestration"));
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IRunQueue, RunQueue>();
builder.Services.AddScoped<IRunOrchestrator, RunOrchestrator>();
builder.Services.Configure<KubernetesProvisionerOptions>(builder.Configuration.GetSection("Kubernetes"));
if (string.Equals(builder.Configuration["Orchestration:Provisioner"], "kubernetes", StringComparison.OrdinalIgnoreCase))
{
    // In-cluster config when running on GKE; falls back to the local kubeconfig.
    builder.Services.AddSingleton<IKubernetes>(_ =>
    {
        var config = KubernetesClientConfiguration.IsInCluster()
            ? KubernetesClientConfiguration.InClusterConfig()
            : KubernetesClientConfiguration.BuildConfigFromConfigFile();
        return new k8s.Kubernetes(config);
    });
    builder.Services.AddScoped<IEnvironmentProvisioner, KubernetesEnvironmentProvisioner>();
}
else
{
    builder.Services.AddScoped<IEnvironmentProvisioner, PlaceholderEnvironmentProvisioner>();
}
// GitHub App Checks API when App credentials are configured; otherwise commit
// statuses (which log when no token is set).
var gitHubOptions = builder.Configuration.GetSection("GitHub").Get<GitHubOptions>() ?? new GitHubOptions();
if (gitHubOptions.UsesApp)
{
    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddScoped<IGitHubChecks, GitHubAppChecks>();
}
else
{
    builder.Services.AddScoped<IGitHubChecks, GitHubChecks>();
}
builder.Services.AddScoped<IReplayClient, GrpcReplayClient>();
builder.Services.AddGrpcClient<ReplayService.ReplayServiceClient>((sp, o) =>
{
    var address = sp.GetRequiredService<IConfiguration>()["Orchestration:ReplayEngineAddress"]
        ?? "http://replay-engine:9090";
    o.Address = new Uri(address);
});
builder.Services.AddScoped<IAnalysisClient, GrpcAnalysisClient>();
builder.Services.AddGrpcClient<AnalysisService.AnalysisServiceClient>((sp, o) =>
{
    var address = sp.GetRequiredService<IConfiguration>()["Orchestration:AnalysisServiceAddress"]
        ?? "http://analysis:9091";
    o.Address = new Uri(address);
});
builder.Services.AddHostedService<RunOrchestrationService>();

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
api.MapCaptureEndpoints();
api.MapRunEndpoints();

// Webhooks are secured by HMAC signature, not the JWT scheme (anonymous).
app.MapGitHubWebhook();

await app.RunAsync();

// Exposed so integration tests can use WebApplicationFactory<Program>.
public partial class Program;
