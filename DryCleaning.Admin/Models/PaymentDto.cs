namespace DryCleaning.Admin.Models;

public sealed class PaymentDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public PaymentMethod Method { get; set; }
    public string? TransactionId { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
