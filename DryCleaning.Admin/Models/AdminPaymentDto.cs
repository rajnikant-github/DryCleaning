namespace DryCleaning.Admin.Models;

public sealed class AdminPaymentDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public string? TransactionId { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
