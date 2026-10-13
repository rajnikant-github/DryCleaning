using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>POST api/customer/orders/{id}/payments/paystack.</summary>
public sealed class InitializePaymentResponse
{
    public Guid PaymentId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string AuthorizationUrl { get; set; } = string.Empty;
    public string AccessCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
}
