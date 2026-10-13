namespace DryCleaning.Models;

public sealed class PaymentResult
{
    public required bool Success { get; init; }
    public required string? Reference { get; init; }
    public required string? Message { get; init; }
}
