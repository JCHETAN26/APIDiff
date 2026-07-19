using System.Net.Http.Headers;
using System.Net.Http.Json;
using ApiDiff.Api.Domain;
using Microsoft.Extensions.Options;

namespace ApiDiff.Api.Orchestration;

/// <summary>
/// Posts a commit status to GitHub for the run's candidate SHA. When no token is
/// configured (e.g. local/dev), the intended status is logged instead.
/// </summary>
public sealed class GitHubChecks(
    IHttpClientFactory httpClientFactory,
    IOptions<GitHubOptions> options,
    ILogger<GitHubChecks> logger) : IGitHubChecks
{
    private const string Context = "apidiff/regression";

    public async Task PostAsync(RegressionRun run, Project project, bool success, string summary, string detailsUrl, CancellationToken ct)
    {
        var state = success ? "success" : "failure";
        var token = options.Value.Token;
        if (string.IsNullOrEmpty(token))
        {
            logger.LogInformation("GitHub check (unconfigured, not posted): {Repo}@{Sha} -> {State}: {Summary}",
                project.GitHubRepo, run.CommitSha, state, summary);
            return;
        }

        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(options.Value.ApiBaseUrl);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("apidiff");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync(
            $"/repos/{project.GitHubRepo}/statuses/{run.CommitSha}",
            new { state, target_url = detailsUrl, description = Truncate(summary, 140), context = Context },
            ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("GitHub status post failed ({Status}) for {Repo}@{Sha}",
                (int)response.StatusCode, project.GitHubRepo, run.CommitSha);
        }
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
