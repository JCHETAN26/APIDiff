namespace ApiDiff.Api.Orchestration;

/// <summary>Background worker that dequeues runs and orchestrates them.</summary>
public sealed class RunOrchestrationService(
    IRunQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<RunOrchestrationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Guid runId;
            try
            {
                runId = await queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                using var scope = scopeFactory.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<IRunOrchestrator>();
                await orchestrator.ExecuteAsync(runId, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error orchestrating run {RunId}", runId);
            }
        }
    }
}
