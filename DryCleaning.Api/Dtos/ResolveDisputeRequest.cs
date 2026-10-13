using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class ResolveDisputeRequest
{
    /// <summary>Resolved or Rejected.</summary>
    public DisputeStatus Status { get; set; }

    public string Resolution { get; set; } = string.Empty;

    /// <summary>Optional: status to move the order to (e.g. Processing to resume, Cancelled, Completed).</summary>
    public OrderStatus? OrderStatus { get; set; }
}
