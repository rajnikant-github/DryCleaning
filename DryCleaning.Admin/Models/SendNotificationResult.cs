namespace DryCleaning.Admin.Models;

public sealed class SendNotificationResult
{
    public int Recipients { get; set; }
    public int EmailsQueued { get; set; }
}
