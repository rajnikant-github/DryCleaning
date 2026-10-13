using System.Text.Json;
using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Infrastructure.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Services;

/// <summary>
/// Paystack checkout:
/// 1. App calls <c>POST /api/customer/orders/{id}/payments/paystack</c> → opens the returned AuthorizationUrl.
/// 2. Paystack calls the webhook (<c>charge.success</c>) and/or the app calls verify with the reference.
/// 3. Payment becomes Success and the provider is notified of the new order.
/// </summary>
public sealed class PaymentService(
    AppDbContext db,
    CurrentProfile profile,
    IPaystackClient paystack,
    IOptions<PaystackOptions> paystackOptions,
    IOptions<MarketplaceOptions> marketplace,
    ILogger<PaymentService> logger)
{
    public async Task<InitializePaymentResponse> InitializePaystackAsync(Guid orderId, CancellationToken ct)
    {
        var customer = await profile.CustomerAsync(ct);
        var order = await db.Orders.Include(o => o.Payments).Include(o => o.Customer).ThenInclude(c => c.User)
                        .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customer.Id, ct)
                    ?? throw new NotFoundException("Order not found.");

        if (order.IsPaid())
            throw new ConflictException("This order has already been paid.");
        if (order.Status != OrderStatus.Created)
            throw new ConflictException("Only new orders can be paid.");

        var user = order.Customer.User;
        var email = user.Email ?? $"{new string((user.Phone ?? user.Id.ToString("N")).Where(char.IsLetterOrDigit).ToArray())}@customers.edrycleaners.co.za";
        var reference = $"{order.OrderNumber}-{Guid.NewGuid():N}"[..Math.Min(50, order.OrderNumber.Length + 33)];
        var currency = marketplace.Value.Currency;

        var result = await paystack.InitializeAsync(email, order.TotalAmount, currency, reference, new Dictionary<string, string>
        {
            ["order_id"] = order.Id.ToString(),
            ["order_number"] = order.OrderNumber
        }, ct);

        var payment = new Payment
        {
            OrderId = order.Id,
            Amount = order.TotalAmount,
            Status = PaymentStatus.Pending,
            Method = PaymentMethod.Card,
            TransactionId = result.Reference
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);

        return new InitializePaymentResponse
        {
            PaymentId = payment.Id,
            Reference = result.Reference,
            AuthorizationUrl = result.AuthorizationUrl,
            AccessCode = result.AccessCode,
            Amount = order.TotalAmount,
            Currency = currency,
            PublicKey = paystackOptions.Value.PublicKey
        };
    }

    /// <summary>Called by the app after checkout returns.</summary>
    public async Task<PaymentDto> VerifyForCustomerAsync(Guid orderId, string reference, CancellationToken ct)
    {
        var customer = await profile.CustomerAsync(ct);
        var payment = await db.Payments.Include(p => p.Order)
                          .FirstOrDefaultAsync(p => p.TransactionId == reference && p.OrderId == orderId && p.Order.CustomerId == customer.Id, ct)
                      ?? throw new NotFoundException("Payment not found.");

        await ApplyVerificationAsync(payment, ct);
        return payment.ToDto();
    }

    public async Task<IReadOnlyList<PaymentDto>> GetForOrderAsync(Guid orderId, CancellationToken ct)
    {
        var customer = await profile.CustomerAsync(ct);
        return (await db.Payments.Where(p => p.OrderId == orderId && p.Order.CustomerId == customer.Id)
                .OrderByDescending(p => p.CreatedAt).ToListAsync(ct))
            .Select(p => p.ToDto()).ToList();
    }

    /// <summary>Handles Paystack webhooks. Returns false when the signature is invalid.</summary>
    public async Task<bool> HandleWebhookAsync(string rawBody, string? signature, CancellationToken ct)
    {
        if (!paystack.IsValidSignature(rawBody, signature))
        {
            logger.LogWarning("Rejected Paystack webhook with invalid signature");
            return false;
        }

        using var json = JsonDocument.Parse(rawBody);
        var root = json.RootElement;
        var eventName = root.TryGetProperty("event", out var e) ? e.GetString() : null;
        var reference = root.TryGetProperty("data", out var data) && data.TryGetProperty("reference", out var r) ? r.GetString() : null;

        logger.LogInformation("Paystack webhook {Event} for {Reference}", eventName, reference);
        if (eventName != "charge.success" || string.IsNullOrEmpty(reference))
            return true; // acknowledge events we don't act on

        var payment = await db.Payments.Include(p => p.Order).FirstOrDefaultAsync(p => p.TransactionId == reference, ct);
        if (payment is null)
        {
            logger.LogWarning("Paystack webhook for unknown reference {Reference}", reference);
            return true;
        }

        await ApplyVerificationAsync(payment, ct);
        return true;
    }

    /// <summary>Always re-verifies with Paystack (never trusts the client or webhook payload alone).</summary>
    private async Task ApplyVerificationAsync(Payment payment, CancellationToken ct)
    {
        if (payment.Status == PaymentStatus.Success || payment.TransactionId is null)
            return;

        var result = await paystack.VerifyAsync(payment.TransactionId, ct);
        var expected = PaystackClient.ToSubunit(payment.Amount);

        if (result.Status == "success" && result.AmountSubunit == expected &&
            string.Equals(result.Currency, marketplace.Value.Currency, StringComparison.OrdinalIgnoreCase))
        {
            payment.Status = PaymentStatus.Success;
            payment.PaidAt = result.PaidAt ?? DateTime.UtcNow;

            var order = payment.Order;
            var providerUserId = await db.ServiceProviders.Where(p => p.Id == order.ServiceProviderId).Select(p => p.UserId).FirstAsync(ct);
            var customerUserId = await db.Customers.Where(c => c.Id == order.CustomerId).Select(c => c.UserId).FirstAsync(ct);
            db.Notify(providerUserId, order, "New order received", $"{order.OrderNumber} is paid and waiting for you to accept.");
            db.Notify(customerUserId, order, "Payment successful", $"Payment for {order.OrderNumber} was successful.", NotificationType.Payment);
        }
        else if (result.Status is "failed" or "abandoned" or "reversed")
        {
            payment.Status = PaymentStatus.Failed;
        }
        else if (result.Status == "success")
        {
            logger.LogError("Paystack amount/currency mismatch for {Reference}: {Amount} {Currency}", payment.TransactionId, result.AmountSubunit, result.Currency);
            payment.Status = PaymentStatus.Failed;
        }

        await db.SaveChangesAsync(ct);
    }
}
