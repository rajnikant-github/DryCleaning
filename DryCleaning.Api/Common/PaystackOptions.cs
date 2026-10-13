using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Common;

public sealed class PaystackOptions
{
    public const string Section = "Paystack";

    public string BaseUrl { get; set; } = "https://api.paystack.co/";
    public string SecretKey { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>Where Paystack redirects after checkout (deep link or web page).</summary>
    public string? CallbackUrl { get; set; }
}
