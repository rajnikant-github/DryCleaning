namespace DryCleaning.Models;

public sealed class Payment : EntityBase
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public PaymentMethod Method { get; set; }
    public string? TransactionId { get; set; }
    public DateTime? PaidAt { get; set; }
}
