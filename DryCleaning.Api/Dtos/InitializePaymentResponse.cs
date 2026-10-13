using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

// Payments (Paystack)

public sealed class InitializePaymentResponse
{
    public required Guid PaymentId { get; init; }
    public required string Reference { get; init; }
    public required string AuthorizationUrl { get; init; }
    public required string AccessCode { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required string PublicKey { get; init; }
}
