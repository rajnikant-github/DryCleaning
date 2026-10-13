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
            return $"{i.Quantity} {product.Name.ToLowerInvariant()}";
        }));
        var job = store.DriverJobs.LastOrDefault(j => j.OrderId == order.Id && j.Status != DriverJobStatus.Cancelled);

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
            DriverName = job is null ? null : store.UserById(store.DriverById(job.DriverId).UserId).Name,
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
        if (job is null)
            return null;

        var driver = store.DriverById(job.DriverId);
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
        var jobs = store.DriverJobs.Where(j => j.OrderId == order.Id).ToList();
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
            PickupDriver = ToAssignment(store, jobs.LastOrDefault(j => j.JobType == DriverJobType.Pickup)),
            DeliveryDriver = ToAssignment(store, jobs.LastOrDefault(j => j.JobType == DriverJobType.Delivery)),
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

public sealed class MockCustomerCatalogService(MockDataStore store) : ICustomerCatalogService
{
    public async Task<IReadOnlyList<Category>> GetCategoriesAsync()
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
            return store.Categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToList();
    }

    public async Task<Category> GetCategoryAsync(Guid categoryId)
    {
        await Task.Delay(100);
        lock (store.Gate)
            return store.Categories.FirstOrDefault(c => c.Id == categoryId) ?? throw new KeyNotFoundException("Category not found.");
    }

    public async Task<IReadOnlyList<ProviderListing>> GetProvidersAsync(Guid categoryId)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            return store.Providers
                .Where(p => p.IsActive && store.ServiceProviderCategories.Any(pc => pc.ServiceProviderId == p.Id && pc.CategoryId == categoryId && pc.IsActive))
                .Select(p => ToListing(p, categoryId))
                .Where(l => l.StartingPrice is not null)
                .OrderBy(l => l.StartingPrice)
                .ToList();
        }
    }

    public async Task<ProviderListing> GetProviderAsync(Guid providerId)
    {
        await Task.Delay(100);
        lock (store.Gate)
            return ToListing(store.ProviderById(providerId), null);
    }

    public async Task<IReadOnlyList<CatalogProduct>> GetProductsAsync(Guid providerId, Guid categoryId)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            return store.Products
                .Where(p => p.ServiceProviderId == providerId && p.CategoryId == categoryId && p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .Select(ToCatalogProduct)
                .Where(p => p.ServiceItems.Count > 0)
                .ToList();
        }
    }

    public async Task<CatalogProduct> GetProductAsync(Guid productId)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
            return ToCatalogProduct(store.Products.FirstOrDefault(p => p.Id == productId) ?? throw new KeyNotFoundException("Product not found."));
    }

    public async Task<IReadOnlyList<PopularServiceItem>> GetPopularItemsAsync(int take)
    {
        await Task.Delay(100);
        lock (store.Gate)
        {
            return store.OrderItems
                .GroupBy(i => i.ServiceItemId)
                .OrderByDescending(g => g.Sum(i => i.Quantity))
                .Select(g => store.ServiceItems.First(s => s.Id == g.Key))
                .Where(s => s.IsActive)
                .Take(take)
                .Select(s =>
                {
                    var product = store.Products.First(p => p.Id == s.ProductId);
                    var provider = store.ProviderById(product.ServiceProviderId);
                    return new PopularServiceItem
                    {
                        ServiceItemId = s.Id,
                        ProductId = product.Id,
                        ProviderId = provider.Id,
                        ProviderName = provider.BusinessName,
                        ProductName = product.Name,
                        ServiceItemName = s.Name,
                        Price = s.Price,
                        EstimatedDays = s.EstimatedDays
                    };
                })
                .ToList();
        }
    }

    private ProviderListing ToListing(Provider provider, Guid? categoryId)
    {
        var items = store.Products
            .Where(p => p.ServiceProviderId == provider.Id && p.IsActive && (categoryId == null || p.CategoryId == categoryId))
            .SelectMany(p => store.ServiceItems.Where(s => s.ProductId == p.Id && s.IsActive))
            .ToList();
        var categoryIds = store.ServiceProviderCategories.Where(pc => pc.ServiceProviderId == provider.Id && pc.IsActive).Select(pc => pc.CategoryId).ToList();

        return new ProviderListing
        {
            Id = provider.Id,
            BusinessName = provider.BusinessName,
            Description = provider.Description,
            Locality = store.AddressById(provider.AddressId).SubLocality,
            Categories = store.Categories.Where(c => categoryIds.Contains(c.Id)).OrderBy(c => c.DisplayOrder).Select(c => c.Name).ToList(),
            StartingPrice = items.Count == 0 ? null : items.Min(s => s.Price),
            FastestDays = items.Count == 0 ? null : items.Min(s => s.EstimatedDays)
        };
    }

    private CatalogProduct ToCatalogProduct(Product product)
    {
        var items = store.ServiceItems.Where(s => s.ProductId == product.Id && s.IsActive).OrderBy(s => s.DisplayOrder).ToList();
        return new CatalogProduct
        {
            Id = product.Id,
            ProviderId = product.ServiceProviderId,
            CategoryId = product.CategoryId,
            CategoryName = store.Categories.First(c => c.Id == product.CategoryId).Name,
            Name = product.Name,
            Description = product.Description,
            StartingPrice = items.Count == 0 ? null : items.Min(s => s.Price),
            ServiceItems = items
        };
    }
}

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
            order.DeliveryFee = settings.Pricing.DefaultDeliveryFee;
            order.Tax = Math.Round(order.SubTotal * settings.Pricing.VatRate, 2, MidpointRounding.AwayFromZero);
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

public sealed class MockCustomerProfileService(MockDataStore store, ISessionService session) : ICustomerProfileService
{
    private Customer Customer => store.Customers.First(c => c.Id == (session.CustomerId ?? store.DemoCustomer.Id));

    public async Task<CustomerProfile> GetProfileAsync()
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var customer = Customer;
            return new CustomerProfile
            {
                User = store.UserById(customer.UserId),
                Address = store.Addresses.FirstOrDefault(a => a.Id == customer.AddressId),
                OrderCount = store.Orders.Count(o => o.CustomerId == customer.Id)
            };
        }
    }

    public async Task UpdateProfileAsync(string name, string? email, string? phone)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var user = store.UserById(Customer.UserId);
            user.Name = name.Trim();
            user.Email = email?.Trim() ?? string.Empty;
            user.Phone = phone?.Trim() ?? string.Empty;
            user.UpdatedAt = DateTime.Now;
        }
    }

    public async Task<Address> SaveAddressAsync(Address address)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            // V1: one address per role – a new row keeps past orders' addresses unchanged.
            var saved = new Address
            {
                GooglePlaceId = address.GooglePlaceId, FormattedAddress = address.FormattedAddress,
                AddressLine1 = address.AddressLine1, AddressLine2 = address.AddressLine2, StreetNumber = address.StreetNumber,
                Route = address.Route, SubLocality = address.SubLocality, Locality = address.Locality,
                AdministrativeAreaLevel1 = address.AdministrativeAreaLevel1, AdministrativeAreaLevel2 = address.AdministrativeAreaLevel2,
                Country = address.Country, CountryCode = address.CountryCode, PostalCode = address.PostalCode,
                Latitude = address.Latitude, Longitude = address.Longitude, PlusCode = address.PlusCode
            };
            store.Addresses.Add(saved);
            Customer.AddressId = saved.Id;
            return saved;
        }
    }
}

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
                return new PaymentResult(true, null, "Already paid.");

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
            return new PaymentResult(true, reference, null);
        }
    }
}

/// <summary>Sample Google Places results around Cape Town (until a Google API key is configured).</summary>
public sealed class MockPlacesService : IPlacesService
{
    private static readonly (string Id, string Number, string Route, string Suburb, string Postal, decimal Lat, decimal Lng)[] Samples =
    [
        ("ChIJ-sample-01", "12", "Regent Road", "Sea Point", "8005", -33.9180000m, 18.3870000m),
        ("ChIJ-sample-02", "8", "Kloof Street", "Gardens", "8001", -33.9310000m, 18.4120000m),
        ("ChIJ-sample-03", "27", "Main Road", "Claremont", "7708", -33.9800000m, 18.4650000m),
        ("ChIJ-sample-04", "3", "Lower Main Road", "Observatory", "7925", -33.9380000m, 18.4700000m),
        ("ChIJ-sample-05", "54", "Somerset Road", "Green Point", "8005", -33.9080000m, 18.4110000m),
        ("ChIJ-sample-06", "19", "Albert Road", "Woodstock", "7925", -33.9270000m, 18.4470000m),
        ("ChIJ-sample-07", "6", "Belmont Road", "Rondebosch", "7700", -33.9580000m, 18.4730000m),
        ("ChIJ-sample-08", "41", "Upper Orange Street", "Oranjezicht", "8001", -33.9400000m, 18.4150000m),
        ("ChIJ-sample-09", "15", "Beach Road", "Mouille Point", "8005", -33.9010000m, 18.4030000m),
        ("ChIJ-sample-10", "22", "Wale Street", "Bo-Kaap", "8001", -33.9210000m, 18.4140000m),
        ("ChIJ-sample-11", "100", "Long Street", "Cape Town City Centre", "8001", -33.9220000m, 18.4180000m),
        ("ChIJ-sample-12", "1", "Dock Road", "V&A Waterfront", "8001", -33.9040000m, 18.4200000m),
    ];

    public async Task<IReadOnlyList<PlacePrediction>> AutocompleteAsync(string query)
    {
        await Task.Delay(200);
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            return [];

        var q = query.Trim();
        return Samples
            .Where(s => $"{s.Number} {s.Route} {s.Suburb} Cape Town".Contains(q, StringComparison.OrdinalIgnoreCase))
            .Take(6)
            .Select(s => new PlacePrediction
            {
                PlaceId = s.Id,
                MainText = $"{s.Number} {s.Route}",
                SecondaryText = $"{s.Suburb}, Cape Town, South Africa"
            })
            .ToList();
    }

    public async Task<Address> GetDetailsAsync(string placeId)
    {
        await Task.Delay(200);
        var s = Samples.FirstOrDefault(x => x.Id == placeId);
        if (s.Id is null)
            throw new KeyNotFoundException("Place not found.");
        return ToAddress(s);
    }

    public async Task<Address?> GetCurrentLocationAddressAsync()
    {
        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                return null;

            var location = await Geolocation.Default.GetLastKnownLocationAsync()
                           ?? await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));
            if (location is null)
                return ToAddress(Samples[10]);

            // Nearest sample address (real reverse-geocoding comes with the Google API key).
            var nearest = Samples.OrderBy(s => Math.Pow((double)s.Lat - location.Latitude, 2) + Math.Pow((double)s.Lng - location.Longitude, 2)).First();
            return ToAddress(nearest);
        }
        catch (Exception)
        {
            return ToAddress(Samples[10]);
        }
    }

    private static Address ToAddress((string Id, string Number, string Route, string Suburb, string Postal, decimal Lat, decimal Lng) s) => new()
    {
        GooglePlaceId = s.Id,
        StreetNumber = s.Number,
        Route = s.Route,
        AddressLine1 = $"{s.Number} {s.Route}",
        SubLocality = s.Suburb,
        Locality = "Cape Town",
        AdministrativeAreaLevel1 = "Western Cape",
        AdministrativeAreaLevel2 = "City of Cape Town",
        Country = "South Africa",
        CountryCode = "ZA",
        PostalCode = s.Postal,
        Latitude = s.Lat,
        Longitude = s.Lng,
        PlusCode = null,
        FormattedAddress = $"{s.Number} {s.Route}, {s.Suburb}, Cape Town, {s.Postal}, South Africa"
    };
}
