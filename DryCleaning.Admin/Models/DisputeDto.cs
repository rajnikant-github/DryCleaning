namespace DryCleaning.Admin.Models;

public sealed class DisputeDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string RaisedByName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DisputeStatus Status { get; set; }
    public string? Resolution { get; set; }
    public DateTime CreatedAt { get; set; }
}
