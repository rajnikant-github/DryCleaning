using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>Order timeline entry.</summary>
public sealed class StatusHistoryDto
{
    public OrderStatus Status { get; set; }
    public string? Notes { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
