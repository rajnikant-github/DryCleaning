namespace DryCleaning.Api.Dtos;

public sealed class SendNotificationResult
{
    /// <summary>How many users got the notification (an in-app notification each, plus a push to their phones).</summary>
    public required int Recipients { get; init; }
}
