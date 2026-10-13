using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class ReceiveOrderRequest
{
    public int ActualItemCount { get; set; }
    public string? Notes { get; set; }
}
