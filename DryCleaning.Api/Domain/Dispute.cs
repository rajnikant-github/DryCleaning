namespace DryCleaning.Api.Domain;

public class Dispute : EntityBase
{
    public Guid OrderId { get; set; }
    public Guid RaisedByUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DisputeStatus Status { get; set; } = DisputeStatus.Open;
    public string? Resolution { get; set; }

    public Order Order { get; set; } = null!;
    public User RaisedByUser { get; set; } = null!;
}
