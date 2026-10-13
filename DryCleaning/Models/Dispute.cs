namespace DryCleaning.Models;

public sealed class Dispute : EntityBase
{
    public Guid OrderId { get; set; }
    public Guid RaisedByUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DisputeStatus Status { get; set; }
    public string? Resolution { get; set; }
}
