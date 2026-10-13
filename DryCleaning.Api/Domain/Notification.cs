namespace DryCleaning.Api.Domain;

public class Notification : EntityBase
{
    public Guid UserId { get; set; }
    public Guid? OrderId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }

    public User User { get; set; } = null!;
    public Order? Order { get; set; }
}
