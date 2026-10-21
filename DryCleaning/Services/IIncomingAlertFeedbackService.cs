namespace DryCleaning.Services;

/// <summary>Plays the incoming-order alert sound and haptic feedback while its full-screen page is visible.</summary>
public interface IIncomingAlertFeedbackService
{
    Task PlayAsync(CancellationToken cancellationToken = default);

    void Stop();
}
