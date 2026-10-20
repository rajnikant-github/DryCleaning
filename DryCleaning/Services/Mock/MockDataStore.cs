using DryCleaning.Configuration;
using DryCleaning.Models;

namespace DryCleaning.Services.Mock;

/// <summary>
/// In-memory tables shaped exactly like the V1 schema, seeded with realistic South African data.
/// Reset on every app launch. Replace the Mock* services with API services to go live.
/// </summary>
public sealed class MockDataStore
{
    private readonly AppSettings _settings;

    public MockDataStore(AppSettings settings)
    {
        _settings = settings;
        Seed();
    }

    public object Gate { get; } = new();

    public List<User> Users { get; } = [];
    public List<Customer> Customers { get; } = [];
    public List<Provider> Providers { get; } = [];
    public List<Driver> Drivers { get; } = [];
    public List<Address> Addresses { get; } = [];
    public List<Category> Categories { get; } = [];
    public List<ServiceProviderCategory> ServiceProviderCategories { get; } = [];

    /// <summary>Pickup hours per provider; a provider without an entry uses <see cref="WorkingHour.DefaultWeek"/>.</summary>
    public Dictionary<Guid, List<WorkingHour>> WorkingHours { get; } = [];
    public List<Product> Products { get; } = [];
    public List<ServiceItem> ServiceItems { get; } = [];
    public List<Order> Orders { get; } = [];
    public List<OrderItem> OrderItems { get; } = [];
    public List<OrderStatusHistory> OrderStatusHistory { get; } = [];
    public List<OrderVerification> OrderVerifications { get; } = [];
    public List<OrderPhoto> OrderPhotos { get; } = [];
    public List<Payment> Payments { get; } = [];
    public List<DriverJob> DriverJobs { get; } = [];
    public List<Dispute> Disputes { get; } = [];
    public List<Notification> Notifications { get; } = [];

    /// <summary>The demo provider account used for sign-in.</summary>
    public Provider DemoProvider => Providers[0];

    public User DemoProviderUser => Users.First(u => u.Id == DemoProvider.UserId);

    /// <summary>The demo customer account (Sipho Ndlovu).</summary>
    public Customer DemoCustomer => Customers[0];

    public User DemoCustomerUser => Users.First(u => u.Id == DemoCustomer.UserId);

    /// <summary>The demo marketplace driver (Bongani Zulu).</summary>
    public Driver DemoDriver => Drivers[0];

    public User DemoDriverUser => Users.First(u => u.Id == DemoDriver.UserId);

    public Provider ProviderById(Guid id) => Providers.First(p => p.Id == id);

    public static Task SimulateLatencyAsync() => Task.Delay(350);

    // ── Lookups ──────────────────────────────────────────────────────────────

    public User UserById(Guid id) => Users.First(u => u.Id == id);
    public Address AddressById(Guid id) => Addresses.First(a => a.Id == id);
    public Customer CustomerById(Guid id) => Customers.First(c => c.Id == id);
    public Driver DriverById(Guid id) => Drivers.First(d => d.Id == id);
    public Order OrderById(Guid id) =>
        Orders.FirstOrDefault(o => o.Id == id) ?? throw new KeyNotFoundException("Order not found.");

    public int NextOrderNumber() =>
        Orders.Select(o => int.TryParse(o.OrderNumber.Replace("EDC-", ""), out var n) ? n : 0).DefaultIfEmpty(10000).Max() + 1;

    // ── Shared write helpers (used by several mock services) ─────────────────

    /// <summary>Publishes a Pending pickup (customer → provider) or delivery (provider → customer) job.</summary>
    public DriverJob PublishDriverJob(Order order, DriverJobType type, DateTime? at = null)
    {
        var providerAddressId = ProviderById(order.ServiceProviderId).AddressId;
        var job = new DriverJob
        {
            OrderId = order.Id,
            JobType = type,
            Status = DriverJobStatus.Pending,
            PickupAddressId = type == DriverJobType.Pickup ? order.PickupAddressId : providerAddressId,
            DeliveryAddressId = type == DriverJobType.Pickup ? providerAddressId : order.PickupAddressId,
            CreatedAt = at ?? DateTime.Now,
            UpdatedAt = at ?? DateTime.Now
        };
        DriverJobs.Add(job);
        return job;
    }

    public void Notify(Guid userId, Order? order, string title, string message, NotificationType type = NotificationType.Order) =>
        Notifications.Add(new Notification { UserId = userId, OrderId = order?.Id, Title = title, Message = message, Type = type });

    public void NotifyAvailableDrivers(Order order, string title, string message)
    {
        foreach (var driver in Drivers.Where(d => d.IsActive && d.IsAvailable))
            Notify(driver.UserId, order, title, message);
    }

    // ── Seed ─────────────────────────────────────────────────────────────────

    private void Seed()
    {
        var now = DateTime.Now;

        // Categories (global, admin-managed)
        var dryCleaning = AddCategory("Dry Cleaning", "Clothes and garments", 1);
        var shoeCleaning = AddCategory("Shoe Cleaning", "Sneakers, leather shoes and boots", 2);

        // Demo provider
        var providerUser = AddUser(UserRole.Provider, "Thandi Mokoena", "thandi@sparkledrycleaners.co.za", "+27 82 555 0142");
        var providerAddress = AddAddress("45", "Bree Street", "Cape Town City Centre", "Cape Town", "8001", -33.9200m, 18.4180m);
        var provider = new Provider
        {
            UserId = providerUser.Id,
            AddressId = providerAddress.Id,
            BusinessName = "Sparkle Dry Cleaners",
            Description = "Premium garment and shoe care in the heart of Cape Town since 2012.",
            Phone = "+27 21 555 0199",
            Email = "hello@sparkledrycleaners.co.za",
            CreatedAt = now.AddYears(-1)
        };
        Providers.Add(provider);
        ServiceProviderCategories.Add(new ServiceProviderCategory { ServiceProviderId = provider.Id, CategoryId = dryCleaning.Id });
        ServiceProviderCategories.Add(new ServiceProviderCategory { ServiceProviderId = provider.Id, CategoryId = shoeCleaning.Id });

        // Provider catalog: Category → Product → ServiceItem
        var shirt = AddProduct(provider, dryCleaning, "Shirt", "Formal and casual shirts", 1,
            ("Regular Dry Clean", 45m, 2), ("Premium Dry Clean", 70m, 2), ("Express Dry Clean", 95m, 1));
        var suit = AddProduct(provider, dryCleaning, "Suit", "Two-piece and three-piece suits", 2,
            ("Regular Dry Clean", 180m, 3), ("Premium Dry Clean", 250m, 3), ("Express Dry Clean", 320m, 1));
        var dress = AddProduct(provider, dryCleaning, "Dress", "Day and evening dresses", 3,
            ("Regular Dry Clean", 90m, 2), ("Premium Dry Clean", 130m, 2), ("Express Dry Clean", 170m, 1));
        var trouser = AddProduct(provider, dryCleaning, "Trouser", "Chinos, formal trousers and slacks", 4,
            ("Regular Dry Clean", 50m, 2), ("Premium Dry Clean", 75m, 2), ("Express Dry Clean", 100m, 1));
        var jacket = AddProduct(provider, dryCleaning, "Jacket", "Blazers, coats and light jackets", 5,
            ("Regular Dry Clean", 120m, 3), ("Premium Dry Clean", 160m, 3), ("Express Dry Clean", 210m, 1));
        var sneakers = AddProduct(provider, shoeCleaning, "Sneakers", "Canvas, mesh and leather sneakers", 1,
            ("Basic Clean", 120m, 3), ("Deep Clean", 180m, 4), ("Whitening Treatment", 220m, 5));
        var leatherShoes = AddProduct(provider, shoeCleaning, "Leather Shoes", "Formal and casual leather shoes", 2,
            ("Clean & Polish", 150m, 3), ("Restoration", 300m, 6));
        var boots = AddProduct(provider, shoeCleaning, "Boots", "Ankle, Chelsea and work boots", 3,
            ("Basic Clean", 160m, 3), ("Deep Clean", 240m, 5));

        // Second provider (so customers can choose)
        var bubblesUser = AddUser(UserRole.Provider, "Naledi Khoza", "naledi@bubblesandco.co.za", "+27 83 555 0177");
        var bubbles = new Provider
        {
            UserId = bubblesUser.Id,
            AddressId = AddAddress("88", "Kloof Street", "Gardens", "Cape Town", "8001", -33.9290m, 18.4110m).Id,
            BusinessName = "Bubbles & Co. Laundry",
            Description = "Eco-friendly dry cleaning and sneaker care in Gardens.",
            Phone = "+27 21 555 0123",
            Email = "hello@bubblesandco.co.za",
            CreatedAt = now.AddMonths(-8)
        };
        Providers.Add(bubbles);
        ServiceProviderCategories.Add(new ServiceProviderCategory { ServiceProviderId = bubbles.Id, CategoryId = dryCleaning.Id });
        ServiceProviderCategories.Add(new ServiceProviderCategory { ServiceProviderId = bubbles.Id, CategoryId = shoeCleaning.Id });
        var bShirt = AddProduct(bubbles, dryCleaning, "Shirt", "Everyday and formal shirts", 1,
            ("Regular Dry Clean", 40m, 2), ("Premium Dry Clean", 65m, 2));
        AddProduct(bubbles, dryCleaning, "Suit", "Business suits", 2,
            ("Regular Dry Clean", 170m, 3), ("Express Dry Clean", 290m, 1));
        AddProduct(bubbles, dryCleaning, "Dress", "Casual and evening dresses", 3,
            ("Regular Dry Clean", 85m, 2));
        var bSneakers = AddProduct(bubbles, shoeCleaning, "Sneakers", "All sneaker types", 1,
            ("Basic Clean", 110m, 3), ("Deep Clean", 170m, 4));

        // Marketplace drivers (no ServiceProviderId)
        var driverBongani = AddDriver("Bongani Zulu", "+27 71 555 0110", "CA 456-218", "Observatory");
        var driverRiaan = AddDriver("Riaan du Plessis", "+27 72 555 0187", "CA 902-334", "Woodstock");
        var driverAyanda = AddDriver("Ayanda Nkosi", "+27 73 555 0165", "CA 118-907", "Salt River");

        // Customers
        var customers = new[]
        {
            AddCustomer("Sipho Ndlovu", "+27 82 555 0101", "12", "Regent Road", "Sea Point", "8005", -33.9180m, 18.3870m),
            AddCustomer("Ayesha Patel", "+27 83 555 0123", "8", "Kloof Street", "Gardens", "8001", -33.9310m, 18.4120m),
            AddCustomer("Johan van der Merwe", "+27 84 555 0145", "27", "Main Road", "Claremont", "7708", -33.9800m, 18.4650m),
            AddCustomer("Lerato Khumalo", "+27 72 555 0167", "3", "Lower Main Road", "Observatory", "7925", -33.9380m, 18.4700m),
            AddCustomer("Michael Botha", "+27 76 555 0189", "54", "Somerset Road", "Green Point", "8005", -33.9080m, 18.4110m),
            AddCustomer("Nomvula Dlamini", "+27 79 555 0112", "19", "Albert Road", "Woodstock", "7925", -33.9270m, 18.4470m),
            AddCustomer("Pieter Venter", "+27 82 555 0134", "6", "Belmont Road", "Rondebosch", "7700", -33.9580m, 18.4730m),
            AddCustomer("Zanele Mthembu", "+27 83 555 0156", "41", "Upper Orange Street", "Oranjezicht", "8001", -33.9400m, 18.4150m),
            AddCustomer("Ryan Pillay", "+27 84 555 0178", "15", "Beach Road", "Mouille Point", "8005", -33.9010m, 18.4030m),
            AddCustomer("Fatima Adams", "+27 71 555 0191", "22", "Wale Street", "Bo-Kaap", "8001", -33.9210m, 18.4140m),
            AddCustomer("Kagiso Molefe", "+27 73 555 0113", "9", "Roeland Street", "Gardens", "8001", -33.9300m, 18.4230m),
            AddCustomer("Emma Smith", "+27 74 555 0135", "31", "High Level Road", "Sea Point", "8005", -33.9150m, 18.3920m),
            AddCustomer("Thabo Mahlangu", "+27 76 555 0157", "11", "Station Road", "Observatory", "7925", -33.9370m, 18.4710m),
        };

        ServiceItem Si(Product p, int index) => ServiceItems.Where(s => s.ProductId == p.Id).OrderBy(s => s.DisplayOrder).ElementAt(index);

        // Orders across every provider-relevant status
        AddOrder(customers[0], provider, OrderStatus.Created, now.AddMinutes(-2), null, null,
            (shirt, Si(shirt, 0), 2), (suit, Si(suit, 1), 1));
        AddOrder(customers[1], provider, OrderStatus.Created, now.AddMinutes(-18), null, null,
            (dress, Si(dress, 1), 2), (sneakers, Si(sneakers, 1), 1));
        AddOrder(customers[2], provider, OrderStatus.PickupAssigned, now.AddMinutes(-55), driverBongani, null,
            (trouser, Si(trouser, 0), 3), (shirt, Si(shirt, 0), 3));
        AddOrder(customers[3], provider, OrderStatus.PickedUp, now.AddHours(-2), driverRiaan, null,
            (shirt, Si(shirt, 1), 3), (trouser, Si(trouser, 1), 2));
        AddOrder(customers[4], provider, OrderStatus.ReceivedByProvider, now.AddHours(-3), driverAyanda, null,
            (suit, Si(suit, 0), 1), (jacket, Si(jacket, 0), 1));
        AddOrder(customers[5], provider, OrderStatus.Processing, now.AddHours(-5), driverBongani, null,
            (shirt, Si(shirt, 0), 4), (trouser, Si(trouser, 0), 2));
        AddOrder(customers[6], provider, OrderStatus.QualityCheck, now.AddHours(-26), driverRiaan, null,
            (leatherShoes, Si(leatherShoes, 0), 2));
        AddOrder(customers[7], provider, OrderStatus.ReadyForDelivery, now.AddHours(-30), driverAyanda, null,
            (dress, Si(dress, 0), 1), (shirt, Si(shirt, 2), 2));
        AddOrder(customers[8], provider, OrderStatus.DeliveryAssigned, now.AddDays(-2), driverBongani, driverRiaan,
            (boots, Si(boots, 1), 1));
        AddOrder(customers[9], provider, OrderStatus.OutForDelivery, now.AddDays(-2).AddHours(-3), driverRiaan, driverAyanda,
            (jacket, Si(jacket, 1), 2));
        AddOrder(customers[10], provider, OrderStatus.Completed, now.AddDays(-3), driverAyanda, driverBongani,
            (shirt, Si(shirt, 0), 5), (trouser, Si(trouser, 0), 3));
        AddOrder(customers[11], provider, OrderStatus.Completed, now.AddDays(-4), driverBongani, driverRiaan,
            (sneakers, Si(sneakers, 2), 2));
        AddOrder(customers[12], provider, OrderStatus.Cancelled, now.AddDays(-4).AddHours(-5), null, null,
            (suit, Si(suit, 2), 1));
        var disputed = AddOrder(customers[5], provider, OrderStatus.Disputed, now.AddDays(-6), driverRiaan, null,
            (dress, Si(dress, 1), 1), (jacket, Si(jacket, 1), 1));

        // Extra history for the demo customer (Sipho)
        AddOrder(customers[0], provider, OrderStatus.OutForDelivery, now.AddDays(-2).AddHours(-1), driverRiaan, driverAyanda,
            (shirt, Si(shirt, 1), 2), (jacket, Si(jacket, 0), 1));
        AddOrder(customers[0], bubbles, OrderStatus.Completed, now.AddDays(-9), driverAyanda, driverBongani,
            (bShirt, Si(bShirt, 0), 4), (bSneakers, Si(bSneakers, 1), 1));
        AddOrder(customers[0], provider, OrderStatus.Cancelled, now.AddDays(-15), null, null,
            (suit, Si(suit, 0), 1));

        // ── Driver marketplace ───────────────────────────────────────────────
        // Pending jobs any online driver can accept.
        var pending1 = AddOrder(customers[1], provider, OrderStatus.Confirmed, now.AddMinutes(-40), null, null,
            (shirt, Si(shirt, 1), 4), (suit, Si(suit, 0), 1));
        PublishDriverJob(pending1, DriverJobType.Pickup, now.AddMinutes(-35));
        var pending2 = AddOrder(customers[12], bubbles, OrderStatus.Confirmed, now.AddMinutes(-25), null, null,
            (bSneakers, Si(bSneakers, 0), 2));
        PublishDriverJob(pending2, DriverJobType.Pickup, now.AddMinutes(-20));
        PublishDriverJob(Orders[7], DriverJobType.Delivery, now.AddMinutes(-15)); // ReadyForDelivery, driver requested
        AddOrder(customers[3], provider, OrderStatus.ReadyForDelivery, now.AddHours(-28), driverAyanda, null,
            (suit, Si(suit, 1), 1));                                                 // provider can still demo "Request Driver"

        // Bongani's completed jobs over the last month (earnings history). Two of them finished today.
        AddOrder(customers[6], provider, OrderStatus.Completed, now.AddHours(-23), driverRiaan, driverBongani,
            (shirt, Si(shirt, 0), 3));
        var historyCustomers = new[] { customers[1], customers[3], customers[4], customers[6], customers[7], customers[9], customers[11], customers[12] };
        for (var i = 0; i < 16; i++)
        {
            var c = historyCustomers[i % historyCustomers.Length];
            var p2 = i % 3 == 0 ? bubbles : provider;
            var (prod, si) = p2 == bubbles ? (bShirt, Si(bShirt, 0)) : (trouser, Si(trouser, 0));
            var created = now.AddDays(-(1.5 + i * 1.8)).Date.AddHours(9 + i % 6);
            AddOrder(c, p2, OrderStatus.Completed, created,
                i % 2 == 0 ? driverBongani : driverAyanda,
                i % 2 == 0 ? driverRiaan : driverBongani,
                (prod, si, 1 + i % 4));
        }

        Disputes.Add(new Dispute
        {
            OrderId = disputed.Id,
            RaisedByUserId = providerUser.Id,
            Reason = "Quantity discrepancy",
            Description = "Customer booked 2 items but only 1 jacket was received. Dress missing from bag.",
            Status = DisputeStatus.UnderReview,
            CreatedAt = now.AddDays(-5)
        });

        // Notifications for the provider
        AddNotification(providerUser, Orders[0], "New order received", $"{Orders[0].OrderNumber} from Sipho Ndlovu is waiting for you to accept.", NotificationType.Order, now.AddMinutes(-2), false);
        AddNotification(providerUser, Orders[1], "New order received", $"{Orders[1].OrderNumber} from Ayesha Patel is waiting for you to accept.", NotificationType.Order, now.AddMinutes(-18), false);
        AddNotification(providerUser, Orders[3], "Driver on the way", $"Riaan du Plessis is bringing {Orders[3].OrderNumber} to your store.", NotificationType.Order, now.AddMinutes(-20), false);
        AddNotification(providerUser, Orders[10], "Payment received", $"Payment for {Orders[10].OrderNumber} was successful.", NotificationType.Payment, now.AddDays(-3), true);
        // Notifications for the demo driver
        var bonganiUser = Users.First(u => u.Id == driverBongani.UserId);
        AddNotification(bonganiUser, pending1, "New pickup job", $"Collect {pending1.OrderNumber} from Ayesha Patel for Sparkle Dry Cleaners.", NotificationType.Order, now.AddMinutes(-35), false);
        AddNotification(bonganiUser, Orders[7], "New delivery job", $"Deliver {Orders[7].OrderNumber} from Sparkle Dry Cleaners.", NotificationType.Order, now.AddMinutes(-15), false);
        AddNotification(bonganiUser, Orders[2], "Job accepted", $"You're collecting {Orders[2].OrderNumber} from Johan van der Merwe.", NotificationType.Order, now.AddMinutes(-45), true);
        AddNotification(bonganiUser, null, "Weekly payout", "Your earnings for last week have been paid into your bank account.", NotificationType.Payment, now.AddDays(-2), true);

        AddNotification(providerUser, disputed, "Dispute under review", $"EDry Cleaners support is reviewing the dispute on {disputed.OrderNumber}.", NotificationType.System, now.AddDays(-5), true);
    }

    private Category AddCategory(string name, string description, int order)
    {
        var category = new Category { Name = name, Description = description, DisplayOrder = order };
        Categories.Add(category);
        return category;
    }

    private User AddUser(UserRole role, string name, string email, string phone)
    {
        var user = new User { Role = role, Name = name, Email = email, Phone = phone, PasswordHash = "mock" };
        Users.Add(user);
        return user;
    }

    private Address AddAddress(string streetNumber, string route, string subLocality, string locality, string postalCode, decimal lat, decimal lng)
    {
        var address = new Address
        {
            GooglePlaceId = $"mock-place-{Guid.NewGuid():N}",
            StreetNumber = streetNumber,
            Route = route,
            AddressLine1 = $"{streetNumber} {route}",
            SubLocality = subLocality,
            Locality = locality,
            AdministrativeAreaLevel1 = "Western Cape",
            AdministrativeAreaLevel2 = "City of Cape Town",
            Country = "South Africa",
            CountryCode = "ZA",
            PostalCode = postalCode,
            Latitude = lat,
            Longitude = lng,
            FormattedAddress = $"{streetNumber} {route}, {subLocality}, {locality}, {postalCode}, South Africa"
        };
        Addresses.Add(address);
        return address;
    }

    private Product AddProduct(Provider provider, Category category, string name, string description, int order,
        params (string Name, decimal Price, int Days)[] serviceItems)
    {
        var product = new Product
        {
            ServiceProviderId = provider.Id,
            CategoryId = category.Id,
            Name = name,
            Description = description,
            DisplayOrder = order
        };
        Products.Add(product);

        var index = 1;
        foreach (var (itemName, price, days) in serviceItems)
        {
            ServiceItems.Add(new ServiceItem
            {
                ProductId = product.Id,
                Name = itemName,
                Description = itemName switch
                {
                    var n when n.StartsWith("Express") => "Priority cleaning, ready next day",
                    var n when n.StartsWith("Premium") => "Hand-finished with stain treatment",
                    var n when n.StartsWith("Regular") => "Standard dry clean and press",
                    _ => "Professional shoe care"
                },
                Price = price,
                EstimatedDays = days,
                DisplayOrder = index++
            });
        }

        return product;
    }

    private Driver AddDriver(string name, string phone, string license, string suburb)
    {
        var user = AddUser(UserRole.Driver, name, $"{name.Split(' ')[0].ToLowerInvariant()}@drivers.edrycleaners.co.za", phone);
        var address = AddAddress("1", "Driver Base Road", suburb, "Cape Town", "7925", -33.93m, 18.46m);
        var driver = new Driver { UserId = user.Id, AddressId = address.Id, LicenseNumber = license };
        Drivers.Add(driver);
        return driver;
    }

    private Customer AddCustomer(string name, string phone, string streetNumber, string route, string suburb, string postalCode, decimal lat, decimal lng)
    {
        var user = AddUser(UserRole.Customer, name, $"{name.Split(' ')[0].ToLowerInvariant()}@example.co.za", phone);
        var address = AddAddress(streetNumber, route, suburb, "Cape Town", postalCode, lat, lng);
        var customer = new Customer { UserId = user.Id, AddressId = address.Id };
        Customers.Add(customer);
        return customer;
    }

    private Order AddOrder(Customer customer, Provider provider, OrderStatus status, DateTime createdAt,
        Driver? pickupDriver, Driver? deliveryDriver, params (Product Product, ServiceItem ServiceItem, int Quantity)[] lines)
    {
        var order = new Order
        {
            OrderNumber = $"EDC-{NextOrderNumber()}",
            CustomerId = customer.Id,
            ServiceProviderId = provider.Id,
            PickupAddressId = customer.AddressId,
            Status = status,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            PickupDate = createdAt.Date.AddDays(createdAt.Hour >= 17 ? 1 : 0),
            PickupSlot = PickupSlots.Label(PickupSlots.All[Math.Abs(createdAt.Minute) % PickupSlots.All.Count])
        };

        foreach (var (product, serviceItem, quantity) in lines)
        {
            OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = product.Id,
                ServiceItemId = serviceItem.Id,
                Quantity = quantity,
                UnitPrice = serviceItem.Price,
                TotalPrice = serviceItem.Price * quantity,
                CreatedAt = createdAt
            });
        }

        order.SubTotal = OrderItems.Where(i => i.OrderId == order.Id).Sum(i => i.TotalPrice);
        order.DeliveryFee = _settings.Pricing.DefaultDeliveryFee;
        order.Tax = Math.Round(order.SubTotal * _settings.Pricing.VatRate, 2);
        order.TotalAmount = order.SubTotal + order.Tax + order.DeliveryFee;
        Orders.Add(order);

        Payments.Add(new Payment
        {
            OrderId = order.Id,
            Amount = order.TotalAmount,
            Method = PaymentMethod.Card,
            Status = PaymentStatus.Success,
            TransactionId = $"TXN{Random.Shared.Next(100000, 999999)}",
            PaidAt = createdAt,
            CreatedAt = createdAt
        });

        // Status history up to the current status
        var reached = status switch
        {
            OrderStatus.Cancelled => new List<OrderStatus> { OrderStatus.Created, OrderStatus.Cancelled },
            OrderStatus.Disputed => new List<OrderStatus> { OrderStatus.Created, OrderStatus.Confirmed, OrderStatus.PickupAssigned, OrderStatus.PickedUp, OrderStatus.ReceivedByProvider, OrderStatus.Disputed },
            _ => OrderWorkflow.Lifecycle.TakeWhile(s => s != status).Append(status).ToList()
        };

        var at = createdAt;
        foreach (var step in reached)
        {
            OrderStatusHistory.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = step,
                ChangedByUserId = provider.UserId,
                Notes = step == OrderStatus.Cancelled ? "Rejected: unable to service this order at the moment." : null,
                CreatedAt = at
            });
            at = at.AddMinutes(step == OrderStatus.Created ? 5 : 25);
            if (at > DateTime.Now) at = DateTime.Now;
        }
        order.UpdatedAt = OrderStatusHistory.Where(h => h.OrderId == order.Id).Max(h => h.CreatedAt);

        var providerAddressId = provider.AddressId;
        if (pickupDriver is not null)
        {
            var pickupDone = status is not (OrderStatus.PickupAssigned or OrderStatus.PickedUp);
            DriverJobs.Add(new DriverJob
            {
                OrderId = order.Id,
                DriverId = pickupDriver.Id,
                JobType = DriverJobType.Pickup,
                Status = pickupDone ? DriverJobStatus.Completed : status == OrderStatus.PickedUp ? DriverJobStatus.InProgress : DriverJobStatus.Assigned,
                PickupAddressId = customer.AddressId,
                DeliveryAddressId = providerAddressId,
                AssignedAt = createdAt.AddMinutes(10),
                CompletedAt = pickupDone ? createdAt.AddMinutes(60) : null,
                CreatedAt = createdAt.AddMinutes(10)
            });
        }

        if (deliveryDriver is not null)
        {
            var deliveryDone = status is OrderStatus.Delivered or OrderStatus.Completed;
            DriverJobs.Add(new DriverJob
            {
                OrderId = order.Id,
                DriverId = deliveryDriver.Id,
                JobType = DriverJobType.Delivery,
                Status = deliveryDone ? DriverJobStatus.Completed : status == OrderStatus.OutForDelivery ? DriverJobStatus.InProgress : DriverJobStatus.Assigned,
                PickupAddressId = providerAddressId,
                DeliveryAddressId = customer.AddressId,
                AssignedAt = createdAt.AddHours(20),
                CompletedAt = deliveryDone ? createdAt.AddHours(22) : null,
                CreatedAt = createdAt.AddHours(20)
            });
        }

        AddCodes(order, customer, status);

        return order;
    }

    /// <summary>Pickup code (QR) and delivery OTP the customer shares with drivers.</summary>
    public void AddCodes(Order order, Customer customer, OrderStatus status)
    {
        var pickedUp = status is not (OrderStatus.Created or OrderStatus.Confirmed or OrderStatus.PickupAssigned or OrderStatus.Cancelled);
        var delivered = status is OrderStatus.Delivered or OrderStatus.Completed;
        OrderVerifications.Add(new OrderVerification
        {
            OrderId = order.Id,
            Type = VerificationType.QR,
            OtpCode = Random.Shared.Next(1000, 9999).ToString(),
            VerifiedAt = pickedUp ? order.CreatedAt.AddHours(1) : null,
            CreatedAt = order.CreatedAt
        });
        OrderVerifications.Add(new OrderVerification
        {
            OrderId = order.Id,
            Type = VerificationType.OTP,
            OtpCode = Random.Shared.Next(1000, 9999).ToString(),
            VerifiedAt = delivered ? order.CreatedAt.AddHours(22) : null,
            VerifiedByUserId = delivered ? customer.UserId : null,
            CreatedAt = order.CreatedAt
        });
    }

    private void AddNotification(User user, Order? order, string title, string message, NotificationType type, DateTime at, bool isRead)
    {
        Notifications.Add(new Notification
        {
            UserId = user.Id,
            OrderId = order?.Id,
            Title = title,
            Message = message,
            Type = type,
            IsRead = isRead,
            CreatedAt = at
        });
    }
}
