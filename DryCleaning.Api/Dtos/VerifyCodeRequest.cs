using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class VerifyCodeRequest
{
    /// <summary>Pickup code shown to the customer (QR) or delivery OTP.</summary>
    public string Code { get; set; } = string.Empty;

    public int? ItemCount { get; set; }
    public string? Notes { get; set; }
}
