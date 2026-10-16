using DryCleaning.Configuration;
using DryCleaning.Helpers;
using DryCleaning.Models;

namespace DryCleaning.Services.Mock;

/// <summary>Shared entity → read-model mapping for the mock services.</summary>
public static class MockMapping
{
    public static OrderSummary ToSummary(MockDataStore store, Order order)
    {
        var customer = store.CustomerById(order.CustomerId);
        var items = store.OrderItems.Where(i => i.OrderId == order.Id).ToList();
        var itemsText = string.Join(", ", items.Select(i =>
        {
            var product = store.Products.First(p => p.Id == i.ProductId);
            return $"{i.Quantity} × {product.Name.ToLowerInvariant()}";
        }));
        var job = store.DriverJobs.LastOrDefault(j => j.OrderId == order.Id && j.DriverId != null && j.Status != DriverJobStatus.Cancelled);

        return new OrderSummary
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerName = store.UserById(customer.UserId).Name,
            ProviderName = store.ProviderById(order.ServiceProviderId).BusinessName,
            Status = order.Status,
            ItemCount = items.Sum(i => i.Quantity),
            ItemsText = itemsText,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            PickupDate = order.PickupDate,
            PickupSlot = order.PickupSlot,
            IsPaid = store.Payments.Any(p => p.OrderId == order.Id && p.Status == PaymentStatus.Success),
            DriverName = job?.DriverId is { } driverId ? store.UserById(store.DriverById(driverId).UserId).Name : null,
            HasOpenDispute = store.Disputes.Any(d => d.OrderId == order.Id && d.Status is DisputeStatus.Open or DisputeStatus.UnderReview)
        };
    }

    public static OrderItemDetail ToItemDetail(MockDataStore store, OrderItem item)
    {
        var product = store.Products.First(p => p.Id == item.ProductId);
        var category = store.Categories.First(c => c.Id == product.CategoryId);
        var serviceItem = store.ServiceItems.First(s => s.Id == item.ServiceItemId);
        return new OrderItemDetail
        {
            Id = item.Id,
            ProductId = product.Id,
            ServiceItemId = serviceItem.Id,
            CategoryName = category.Name,
            ProductName = product.Name,
            ServiceItemName = serviceItem.Name,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            TotalPrice = item.TotalPrice,
            Notes = item.Notes,
            EstimatedDays = serviceItem.EstimatedDays,
            Icon = CatalogIcons.ForProduct(product.Name)
        };
    }

    public static DriverAssignment? ToAssignment(MockDataStore store, DriverJob? job)
    {
        if (job?.DriverId is not { } driverId || job.Status == DriverJobStatus.Cancelled)
            return null;

        var driver = store.DriverById(driverId);
        var user = store.UserById(driver.UserId);
        return new DriverAssignment
        {
            DriverJobId = job.Id,
            JobType = job.JobType,
            Status = job.Status,
            DriverName = user.Name,
            DriverPhone = user.Phone,
            LicenseNumber = driver.LicenseNumber,
            AssignedAt = job.AssignedAt,
            CompletedAt = job.CompletedAt
        };
    }

    public static OrderDetail ToDetail(MockDataStore store, Order order, bool includeCustomerCodes)
    {
        var customer = store.CustomerById(order.CustomerId);
        var customerUser = store.UserById(customer.UserId);
        var provider = store.ProviderById(order.ServiceProviderId);
        var jobs = store.DriverJobs.Where(j => j.OrderId == order.Id && j.Status != DriverJobStatus.Cancelled).ToList();
        var codes = store.OrderVerifications.Where(v => v.OrderId == order.Id && v.VerifiedAt == null).ToList();

        return new OrderDetail
        {
            Order = order,
            CustomerName = customerUser.Name,
            CustomerPhone = customerUser.Phone,
            PickupAddress = store.AddressById(order.PickupAddressId),
            Items = store.OrderItems.Where(i => i.OrderId == order.Id).Select(i => ToItemDetail(store, i)).ToList(),
            History = store.OrderStatusHistory.Where(h => h.OrderId == order.Id).OrderBy(h => h.CreatedAt).ToList(),
            Photos = store.OrderPhotos.Where(p => p.OrderId == order.Id).OrderBy(p => p.CreatedAt).ToList(),
            PickupDriver = ToAssignment(store, jobs.LastOrDefault(j => j.JobType == DriverJobType.Pickup && j.DriverId != null)),
            DeliveryDriver = ToAssignment(store, jobs.LastOrDefault(j => j.JobType == DriverJobType.Delivery && j.DriverId != null)),
            AwaitingDriverFor = jobs.LastOrDefault(j => j.Status == DriverJobStatus.Pending)?.JobType,
            Payment = store.Payments.LastOrDefault(p => p.OrderId == order.Id && p.Status == PaymentStatus.Success)
                      ?? store.Payments.LastOrDefault(p => p.OrderId == order.Id),
            OpenDispute = store.Disputes.LastOrDefault(d => d.OrderId == order.Id && d.Status is DisputeStatus.Open or DisputeStatus.UnderReview),
            ProviderName = provider.BusinessName,
            ProviderPhone = provider.Phone,
            ProviderAddress = store.AddressById(provider.AddressId),
            PickupCode = includeCustomerCodes ? codes.FirstOrDefault(v => v.Type == VerificationType.QR)?.OtpCode : null,
            DeliveryOtp = includeCustomerCodes ? codes.FirstOrDefault(v => v.Type == VerificationType.OTP)?.OtpCode : null
        };
    }
}
