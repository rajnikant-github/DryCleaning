namespace DryCleaning.Api.Dtos;

public sealed class SendNotificationResult
{
    /// <summary>How many users got the notification (an in-app notification each, plus a push to their phones).</summary>
    public required int Recipients { get; init; }

    /// <summary>How many announcement emails were queued (when "also email" was chosen).</summary>
    public int EmailsQueued { get; init; }
}
