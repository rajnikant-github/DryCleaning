using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class PaymentDto
{
    public required Guid Id { get; init; }
    public required decimal Amount { get; init; }
    public required PaymentStatus Status { get; init; }
    public required PaymentMethod Method { get; init; }
    public required string? TransactionId { get; init; }
    public required DateTime? PaidAt { get; init; }
    public required DateTime CreatedAt { get; init; }
}
