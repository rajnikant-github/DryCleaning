using DryCleaning.Configuration;
using DryCleaning.Helpers;
using DryCleaning.Models;

namespace DryCleaning.Services.Mock;

public sealed class MockCustomerOrderService(MockDataStore store, ISessionService session, AppSettings settings) : ICustomerOrderService
{
    private Customer Customer => store.Customers.First(c => c.Id == (session.CustomerId ?? store.DemoCustomer.Id));

    public async Task<CustomerHomeData> GetHomeAsync()
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var customer = Customer;
            var user = store.UserById(customer.UserId);
            var orders = store.Orders.Where(o => o.CustomerId == customer.Id).OrderByDescending(o => o.UpdatedAt).ToList();
            var active = orders.FirstOrDefault(o => IsActive(o.Status));

            var popular = store.OrderItems
                .GroupBy(i => i.ServiceItemId)
                .OrderByDescending(g => g.Sum(i => i.Quantity))
                .Select(g => store.ServiceItems.First(s => s.Id == g.Key))
                .Where(s => s.IsActive)
                .Take(4)
                .Select(s =>
                {
                    var product = store.Products.First(p => p.Id == s.ProductId);
                    var provider = store.ProviderById(product.ServiceProviderId);
                    return new PopularServiceItem
                    {
                        ServiceItemId = s.Id, ProductId = product.Id, ProviderId = provider.Id, ProviderName = provider.BusinessName,
                        ProductName = product.Name, ServiceItemName = s.Name, Price = s.Price, EstimatedDays = s.EstimatedDays
                    };
                })
                .ToList();

            return new CustomerHomeData
            {
                FirstName = user.Name.Split(' ')[0],
                Address = customer.AddressId == Guid.Empty ? null : store.AddressById(customer.AddressId),
                Categories = store.Categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToList(),
                ActiveOrder = active is null ? null : MockMapping.ToSummary(store, active),
                RecentOrders = orders.Where(o => o.Id != active?.Id).Take(3).Select(o => MockMapping.ToSummary(store, o)).ToList(),
                PopularItems = popular,
                UnreadNotifications = store.Notifications.Count(n => n.UserId == user.Id && !n.IsRead)
            };
        }
    }

    public async Task<IReadOnlyList<OrderSummary>> GetOrdersAsync(CustomerOrderFilter filter)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var customerId = Customer.Id;
            return store.Orders
                .Where(o => o.CustomerId == customerId && Matches(filter, o.Status))
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => MockMapping.ToSummary(store, o))
                .ToList();
        }
    }

    public Task<IReadOnlyDictionary<CustomerOrderFilter, int>> GetOrderCountsAsync()
    {
        lock (store.Gate)
        {
            var customerId = Customer.Id;
            var orders = store.Orders.Where(o => o.CustomerId == customerId).ToList();
            IReadOnlyDictionary<CustomerOrderFilter, int> counts = Enum.GetValues<CustomerOrderFilter>()
                .ToDictionary(f => f, f => orders.Count(o => Matches(f, o.Status)));
            return Task.FromResult(counts);
        }
    }

    public async Task<OrderDetail> GetOrderAsync(Guid orderId)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var order = store.OrderById(orderId);
            if (order.CustomerId != Customer.Id)
                throw new KeyNotFoundException("Order not found.");
            return MockMapping.ToDetail(store, order, includeCustomerCodes: true);
        }
    }

    public Task<OrderQuote?> QuoteAsync(ICartService cart)
    {
        if (cart.IsEmpty || cart.ProviderId is null || cart.PickupAddress is null)
            return Task.FromResult<OrderQuote?>(null);

        lock (store.Gate)
        {
            var km = DistanceKm(cart.PickupAddress, cart.ProviderId.Value);
            var fee = settings.Pricing.Delivery.FeeFor(km);
            var tax = Math.Round(cart.SubTotal * settings.Pricing.VatRate, 2, MidpointRounding.AwayFromZero);
            return Task.FromResult<OrderQuote?>(new OrderQuote
            {
                SubTotal = cart.SubTotal,
                Tax = tax,
                VatRate = settings.Pricing.VatRate * 100m,
                DeliveryFee = fee,
                Total = cart.SubTotal + tax + fee,
                DistanceKm = (decimal)km,
                DistanceIsEstimate = true
            });
        }
    }

    private double DistanceKm(Address pickup, Guid providerId) =>
        store.ProviderById(providerId).AddressId is { } addressId
            ? Helpers.Geo.DistanceKm(pickup, store.AddressById(addressId))
            : 0;

    public async Task<Guid> PlaceOrderAsync(ICartService cart)
    {
        await MockDataStore.SimulateLatencyAsync();
        if (cart.IsEmpty || cart.ProviderId is null)
            throw new InvalidOperationException("Your cart is empty.");
        if (cart.PickupAddress is null)
            throw new InvalidOperationException("Choose a pickup address.");
        if (cart.PickupDate is null || string.IsNullOrEmpty(cart.PickupSlot))
            throw new InvalidOperationException("Choose a pickup time.");

        lock (store.Gate)
        {
            var customer = Customer;
            var address = cart.PickupAddress;
            if (store.Addresses.All(a => a.Id != address.Id))
                store.Addresses.Add(address);

            var order = new Order
            {
                OrderNumber = $"EDC-{store.NextOrderNumber()}",
                CustomerId = customer.Id,
                ServiceProviderId = cart.ProviderId.Value,
                PickupAddressId = address.Id,
                Status = OrderStatus.Created,
                PickupDate = cart.PickupDate,
                PickupSlot = cart.PickupSlot
            };

            foreach (var line in cart.Lines)
            {
                // Server-side pricing: always use the current catalog price.
                var serviceItem = store.ServiceItems.FirstOrDefault(s => s.Id == line.ServiceItemId && s.IsActive)
                                  ?? throw new InvalidOperationException($"{line.ProductName} – {line.ServiceItemName} is no longer available.");
                store.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = line.ProductId,
                    ServiceItemId = serviceItem.Id,
                    Quantity = line.Quantity,
                    UnitPrice = serviceItem.Price,
                    TotalPrice = serviceItem.Price * line.Quantity,
                    Notes = string.IsNullOrWhiteSpace(line.Notes) ? null : line.Notes.Trim()
                });
                if (!string.IsNullOrEmpty(line.PhotoPath))
                    store.OrderPhotos.Add(new OrderPhoto { OrderId = order.Id, Type = PhotoType.Pickup, Url = line.PhotoPath, UploadedBy = UploadedBy.Customer });
            }

            order.SubTotal = store.OrderItems.Where(i => i.OrderId == order.Id).Sum(i => i.TotalPrice);
            order.DeliveryFee = settings.Pricing.Delivery.FeeFor(DistanceKm(cart.PickupAddress, cart.ProviderId.Value));
            order.Tax = Math.Round(order.SubTotal * settings.Pricing.VatRate, 2, MidpointRounding.AwayFromZero);
            order.VatRate = settings.Pricing.VatRate * 100m;
            order.TotalAmount = order.SubTotal + order.Tax + order.DeliveryFee;
            store.Orders.Add(order);
            store.OrderStatusHistory.Add(new OrderStatusHistory { OrderId = order.Id, Status = OrderStatus.Created, ChangedByUserId = customer.UserId });
            store.AddCodes(order, customer, OrderStatus.Created);
            return order.Id;
        }
    }

    public async Task CancelOrderAsync(Guid orderId, string reason)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var order = store.OrderById(orderId);
            if (order.Status is not (OrderStatus.Created or OrderStatus.Confirmed or OrderStatus.PickupAssigned))
                throw new InvalidOperationException("This order can no longer be cancelled because the items have been collected.");

            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.Now;
            store.OrderStatusHistory.Add(new OrderStatusHistory { OrderId = order.Id, Status = OrderStatus.Cancelled, ChangedByUserId = Customer.UserId, Notes = $"Cancelled by customer: {reason}" });
            foreach (var job in store.DriverJobs.Where(j => j.OrderId == order.Id && j.Status != DriverJobStatus.Completed))
                job.Status = DriverJobStatus.Cancelled;
        }
    }

    public async Task RaiseDisputeAsync(Guid orderId, string reason, string description)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var order = store.OrderById(orderId);
            OrderWorkflow.EnsureCanTransition(order.Status, OrderStatus.Disputed);
            store.Disputes.Add(new Dispute { OrderId = order.Id, RaisedByUserId = Customer.UserId, Reason = reason, Description = description, Status = DisputeStatus.Open });
            order.Status = OrderStatus.Disputed;
            order.UpdatedAt = DateTime.Now;
            store.OrderStatusHistory.Add(new OrderStatusHistory { OrderId = order.Id, Status = OrderStatus.Disputed, ChangedByUserId = Customer.UserId, Notes = $"Dispute raised by customer: {reason}" });
        }
    }

    public static bool IsActive(OrderStatus status) =>
        status is not (OrderStatus.Completed or OrderStatus.Delivered or OrderStatus.Cancelled);

    private static bool Matches(CustomerOrderFilter filter, OrderStatus status) => filter switch
    {
        CustomerOrderFilter.Active => IsActive(status),
        CustomerOrderFilter.Completed => status is OrderStatus.Completed or OrderStatus.Delivered,
        CustomerOrderFilter.Cancelled => status == OrderStatus.Cancelled,
        _ => false
    };
}
