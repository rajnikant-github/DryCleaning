using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>Provider dashboard activity row.</summary>
public sealed class ActivityDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime At { get; set; }
}
