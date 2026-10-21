using DryCleaning.Api.Common;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Infrastructure.Email;

/// <summary>Background worker: sends queued emails one at a time, retrying a failed send a couple of times.</summary>
public sealed class EmailDispatcher(EmailQueue queue, IEmailSender sender, IOptions<EmailOptions> options, ILogger<EmailDispatcher> logger)
    : BackgroundService
{
    private static readonly TimeSpan[] RetryDelays = [TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(15)];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in queue.ReadAllAsync(stoppingToken))
        {
            if (!options.Value.Enabled)
                continue;

            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    await sender.SendAsync(message, stoppingToken);
                    break;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    if (attempt >= RetryDelays.Length)
                    {
                        logger.LogError(ex, "Email to {To} (\"{Subject}\") failed after {Attempts} attempts", message.To, message.Subject, attempt + 1);
                        break;
                    }

                    logger.LogWarning("Email to {To} failed ({Reason}); retrying", message.To, ex.Message);
                    await Task.Delay(RetryDelays[attempt], stoppingToken);
                }
            }
        }
    }
}
