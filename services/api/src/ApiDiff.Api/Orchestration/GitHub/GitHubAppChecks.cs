using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using ApiDiff.Api.Domain;
using Microsoft.Extensions.Options;

namespace ApiDiff.Api.Orchestration.GitHub;

/// <summary>
/// Posts a rich GitHub <b>Check Run</b> for a run's commit, authenticating as a
/// GitHub App: sign an App JWT, exchange it for an installation token, then call
/// the Checks API. Used when App credentials are configured (see
/// <see cref="GitHubOptions.UsesApp"/>).
/// </summary>
public sealed class GitHubAppChecks(
    IHttpClientFactory httpClientFactory,
    IOptions<GitHubOptions> options,
    ILogger<GitHubAppChecks> logger,
    TimeProvider timeProvider) : IGitHubChecks
{
    private const string CheckName = "APIDiff regression";

    public async Task PostAsync(RegressionRun run, Project project, bool success, string summary, string detailsUrl, CancellationToken ct)
    {
        var opts = options.Value;
        using var rsa = RSA.Create();
        rsa.ImportFromPem(opts.PrivateKeyPem);

        var appJwt = GitHubAppJwt.Create(opts.AppId!, rsa, timeProvider.GetUtcNow());
        var client = httpClientFactory.CreateClient("github");
        client.BaseAddress ??= new Uri(opts.ApiBaseUrl);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("apidiff");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

        var installationId = opts.InstallationId != 0
            ? opts.InstallationId
            : await ResolveInstallationIdAsync(client, appJwt, project.GitHubRepo, ct);

        var token = await GetInstallationTokenAsync(client, appJwt, installationId, ct);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.PostAsJsonAsync(
            $"/repos/{project.GitHubRepo}/check-runs",
            new
            {
                name = CheckName,
                head_sha = run.CommitSha,
                status = "completed",
                conclusion = success ? "success" : "failure",
                details_url = detailsUrl,
                output = new { title = success ? "No regressions" : "Regressions detected", summary },
            },
            ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("GitHub check-run post failed ({Status}) for {Repo}@{Sha}",
                (int)response.StatusCode, project.GitHubRepo, run.CommitSha);
        }
    }

    private static async Task<long> ResolveInstallationIdAsync(HttpClient client, string appJwt, string repo, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/repos/{repo}/installation");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", appJwt);
        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return doc.RootElement.GetProperty("id").GetInt64();
    }

    private static async Task<string> GetInstallationTokenAsync(HttpClient client, string appJwt, long installationId, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/app/installations/{installationId}/access_tokens");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", appJwt);
        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return doc.RootElement.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("GitHub installation token response had no token.");
    }
}
