using DryCleaning.Configuration;
using DryCleaning.Helpers;
using DryCleaning.Models;

namespace DryCleaning.Services.Mock;

/// <summary>Simulates Paystack checkout. Replace with a WebView checkout + API verify when going live.</summary>
public sealed class MockPaymentService(MockDataStore store) : IPaymentService
{
    public async Task<PaymentResult> PayAsync(Guid orderId)
    {
        await Task.Delay(2000);
        lock (store.Gate)
        {
            var order = store.OrderById(orderId);
            if (store.Payments.Any(p => p.OrderId == orderId && p.Status == PaymentStatus.Success))
                return new PaymentResult { Success = true, Reference = null, Message = "Already paid." };

            var reference = $"{order.OrderNumber}-{Guid.NewGuid():N}"[..24];
            store.Payments.Add(new Payment
            {
                OrderId = order.Id,
                Amount = order.TotalAmount,
                Method = PaymentMethod.Card,
                Status = PaymentStatus.Success,
                TransactionId = reference,
                PaidAt = DateTime.Now
            });

            var provider = store.ProviderById(order.ServiceProviderId);
            store.Notifications.Add(new Notification
            {
                UserId = provider.UserId,
                OrderId = order.Id,
                Title = "New order received",
                Message = $"{order.OrderNumber} is paid and waiting for you to accept.",
                Type = NotificationType.Order
            });
            return new PaymentResult { Success = true, Reference = reference, Message = null };
        }
    }
}
