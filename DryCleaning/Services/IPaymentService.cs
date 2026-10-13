using DryCleaning.Models;

namespace DryCleaning.Services;

/// <summary>Paystack checkout (mock now; WebView + API verification when live).</summary>
public interface IPaymentService
{
    Task<PaymentResult> PayAsync(Guid orderId);
}
