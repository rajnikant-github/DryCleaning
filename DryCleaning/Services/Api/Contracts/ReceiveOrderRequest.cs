using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>POST api/provider/orders/{id}/receive.</summary>
public sealed class ReceiveOrderRequest
{
    public int ActualItemCount { get; set; }
    public string? Notes { get; set; }
}
