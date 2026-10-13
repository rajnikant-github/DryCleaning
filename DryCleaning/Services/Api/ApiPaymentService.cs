using DryCleaning.Configuration;
using DryCleaning.Models;

namespace DryCleaning.Services.Api;

/// <summary>
/// Paystack checkout: the API starts the transaction, the customer pays on Paystack's secure page
/// (system browser), Paystack redirects to edrycleaners://payment-complete, and the API verifies it.
/// </summary>
public sealed class ApiPaymentService(ApiClient api, AppSettings settings) : IPaymentService
{
    private static ApiJsonContext Json => ApiJsonContext.Default;

    public async Task<PaymentResult> PayAsync(Guid orderId)
    {
        var payments = await api.GetAsync($"api/customer/orders/{orderId}/payments", Json.ListPaymentDto);
        if (payments.Any(p => p.Status == PaymentStatus.Success))
            return new PaymentResult { Success = true, Reference = payments.First(p => p.Status == PaymentStatus.Success).TransactionId, Message = "Already paid." };

        var checkout = await api.PostAsync($"api/customer/orders/{orderId}/payments/paystack", Json.InitializePaymentResponse);

        try
        {
            await WebAuthenticator.Default.AuthenticateAsync(new WebAuthenticatorOptions
            {
                Url = new Uri(checkout.AuthorizationUrl),
                CallbackUrl = new Uri(settings.Api.PaymentCallbackUrl),
                PrefersEphemeralWebBrowserSession = true
            });
        }
        catch (OperationCanceledException)
        {
            // The customer closed the payment page. They may still have paid, so verify below.
        }

        var payment = await api.GetAsync($"api/customer/orders/{orderId}/payments/verify?reference={Uri.EscapeDataString(checkout.Reference)}", Json.PaymentDto);
        return payment.Status switch
        {
            PaymentStatus.Success => new PaymentResult { Success = true, Reference = payment.TransactionId ?? checkout.Reference, Message = null },
            PaymentStatus.Failed => new PaymentResult { Success = false, Reference = checkout.Reference, Message = "Your payment was declined. Please try again or use another card." },
            _ => new PaymentResult { Success = false, Reference = checkout.Reference, Message = "Payment wasn't completed. Tap Pay to try again." }
        };
    }
}
