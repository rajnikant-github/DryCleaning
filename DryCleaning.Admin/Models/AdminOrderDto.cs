namespace DryCleaning.Admin.Models;

public sealed class AdminOrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string? DriverName { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
