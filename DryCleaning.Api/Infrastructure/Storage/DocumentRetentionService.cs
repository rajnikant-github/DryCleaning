using DryCleaning.Api.Services;

namespace DryCleaning.Api.Infrastructure.Storage;

/// <summary>Once a day, deletes verification documents that are past their retention period (see Verification:RetentionMonths).</summary>
public sealed class DocumentRetentionService(IServiceScopeFactory scopes, ILogger<DocumentRetentionService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // let the API finish starting first
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<VerificationDocumentService>().PurgeAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Verification document clean-up failed");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // shutting down
        }
    }
}
