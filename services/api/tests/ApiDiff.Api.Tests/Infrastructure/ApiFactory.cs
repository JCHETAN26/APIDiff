using ApiDiff.Api.Orchestration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace ApiDiff.Api.Tests.Infrastructure;

/// <summary>
/// Spins the API against a real Postgres (Testcontainers), swaps the JWT scheme
/// for <see cref="TestAuthHandler"/>, and substitutes in-memory fakes for the
/// external orchestration dependencies (replay engine, K8s, GitHub).
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string WebhookSecret = "test-webhook-secret";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _postgres.GetConnectionString(),
                ["GitHub:WebhookSecret"] = WebhookSecret,
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            services.AddSingleton<FakeReplayClient>();
            services.AddSingleton<IReplayClient>(sp => sp.GetRequiredService<FakeReplayClient>());
            services.AddSingleton<FakeAnalysisClient>();
            services.AddSingleton<IAnalysisClient>(sp => sp.GetRequiredService<FakeAnalysisClient>());
            services.AddSingleton<FakeEnvironmentProvisioner>();
            services.AddSingleton<IEnvironmentProvisioner>(sp => sp.GetRequiredService<FakeEnvironmentProvisioner>());
            services.AddSingleton<RecordingGitHubChecks>();
            services.AddSingleton<IGitHubChecks>(sp => sp.GetRequiredService<RecordingGitHubChecks>());
        });
    }
}
