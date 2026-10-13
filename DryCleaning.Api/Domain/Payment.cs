namespace DryCleaning.Api.Domain;

public class Payment : EntityBase
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public PaymentMethod Method { get; set; } = PaymentMethod.Card;

    /// <summary>Paystack transaction reference.</summary>
    public string? TransactionId { get; set; }
    public DateTime? PaidAt { get; set; }

    public Order Order { get; set; } = null!;
}
