namespace DryCleaning.Admin.Models;

public sealed class ResolveDisputeRequest
{
    public DisputeStatus Status { get; set; } = DisputeStatus.Resolved;
    public string Resolution { get; set; } = string.Empty;
    public OrderStatus? OrderStatus { get; set; }
}
