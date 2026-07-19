using System.Text;
using System.Text.Json;
using ApiDiff.Api.Auth;
using ApiDiff.Api.Domain;
using ApiDiff.Api.Orchestration;
using ApiDiff.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ApiDiff.Api.Webhooks;

/// <summary>
/// GitHub webhook intake: verifies the HMAC signature, creates a regression run
/// for relevant pull-request events, and enqueues it. Anonymous (secured by the
/// signature, not the JWT scheme) and idempotent per (project, PR, commit).
/// </summary>
public static class GitHubWebhookEndpoint
{
    private static readonly string[] TriggeringActions = ["opened", "synchronize", "reopened"];

    public static void MapGitHubWebhook(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/webhooks/github", Handle).WithTags("Webhooks");
    }

    private static async Task<IResult> Handle(
        HttpContext context,
        IOptions<GitHubOptions> gitHubOptions,
        ApiDiffDbContext db,
        IRunQueue queue,
        IAuditService audit,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("GitHubWebhook");

        using var reader = new StreamReader(context.Request.Body);
        var payload = await reader.ReadToEndAsync(ct);

        var signature = context.Request.Headers["X-Hub-Signature-256"].ToString();
        if (!GitHubSignatureVerifier.IsValid(gitHubOptions.Value.WebhookSecret, Encoding.UTF8.GetBytes(payload), signature))
        {
            return Results.Unauthorized();
        }

        if (context.Request.Headers["X-GitHub-Event"].ToString() != "pull_request")
        {
            return Results.NoContent();
        }

        PullRequestEvent evt;
        try
        {
            evt = ParseEvent(payload);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return Results.BadRequest(new { message = "Malformed pull_request payload." });
        }

        if (!TriggeringActions.Contains(evt.Action))
        {
            return Results.NoContent();
        }

        var project = await db.Projects.FirstOrDefaultAsync(p => p.GitHubRepo == evt.Repo, ct);
        if (project is null)
        {
            logger.LogInformation("Webhook for unknown repo {Repo}; ignoring", evt.Repo);
            return Results.NoContent();
        }

        // Idempotent: a run for this (project, PR, commit) may already exist.
        var existing = await db.RegressionRuns.FirstOrDefaultAsync(
            r => r.ProjectId == project.Id && r.PullRequestNumber == evt.Number && r.CommitSha == evt.Sha, ct);
        if (existing is not null)
        {
            return Results.Accepted($"/api/v1/organizations/{project.OrganizationId}/projects/{project.Id}/runs/{existing.Id}");
        }

        var run = new RegressionRun
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            PullRequestNumber = evt.Number,
            CommitSha = evt.Sha,
            Status = RunStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.RegressionRuns.Add(run);
        audit.Append(project.OrganizationId, null, "run.created", nameof(RegressionRun), run.Id.ToString(),
            new { evt.Repo, evt.Number, evt.Sha });
        await db.SaveChangesAsync(ct);

        queue.Enqueue(run.Id);

        return Results.Accepted($"/api/v1/organizations/{project.OrganizationId}/projects/{project.Id}/runs/{run.Id}");
    }

    private static PullRequestEvent ParseEvent(string payload)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;
        return new PullRequestEvent(
            Action: root.GetProperty("action").GetString() ?? "",
            Number: root.GetProperty("number").GetInt32(),
            Sha: root.GetProperty("pull_request").GetProperty("head").GetProperty("sha").GetString() ?? "",
            Repo: root.GetProperty("repository").GetProperty("full_name").GetString() ?? "");
    }

    private sealed record PullRequestEvent(string Action, int Number, string Sha, string Repo);
}
