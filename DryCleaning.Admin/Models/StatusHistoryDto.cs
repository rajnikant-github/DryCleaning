namespace DryCleaning.Admin.Models;

public sealed class StatusHistoryDto
{
    public OrderStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
