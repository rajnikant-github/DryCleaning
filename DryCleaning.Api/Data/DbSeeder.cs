using System.Security.Cryptography;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Data;

/// <summary>
/// Creates the database and seeds demo data (Cape Town) when empty.
/// Demo logins (password for all: Demo@12345):
///   admin@edrycleaners.co.za · thandi@sparkledrycleaners.co.za · bongani@drivers.edrycleaners.co.za · sipho@example.co.za
/// </summary>
public static class DbSeeder
{
    public const string DemoPassword = "Demo@12345";

    public static async Task InitialiseAsync(IServiceProvider services, bool seedDemoData, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        // Apply migrations if the project has them; otherwise create the schema directly.
        if (db.Database.GetMigrations().Any())
        {
            try
            {
                await db.Database.MigrateAsync(ct);
            }
            catch (InvalidOperationException ex) when (ex.Message?.Contains("PendingModelChangesWarning") == true || ex.Message?.Contains("pending changes") == true)
            {
                // Migration failed because the EF model has changed but no migration was added.
                // Log a clear error and skip seeding so the app can start. Developers should add a migration.
                logger.LogError(ex, "EF Core model has pending changes; migrations were not applied. Add a new migration (dotnet ef migrations add <Name>) and apply it during deployment.");
                return; // don't seed demo data when migrations couldn't be applied
            }
        }
        else
        {
            await db.Database.EnsureCreatedAsync(ct);
        }

        await DocumentTypeSeeder.EnsureDefaultsAsync(db, ct);

        if (!seedDemoData || await db.Users.AnyAsync(ct))
            return;

        logger.LogInformation("Seeding demo data…");
        await SeedAsync(db, hasher, ct);
        logger.LogInformation("Demo data seeded. Password for all demo users: {Password}", DemoPassword);
    }

    private static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher, CancellationToken ct)
    {
        var hash = hasher.Hash(DemoPassword);

        User NewUser(UserRole role, string name, string email, string phone) =>
            new() { Role = role, Name = name, Email = email, Phone = phone, PasswordHash = hash };

        Address NewAddress(string number, string route, string suburb, string postal, decimal lat, decimal lng) => new()
        {
            GooglePlaceId = $"seed-{Guid.NewGuid():N}",
            StreetNumber = number,
            Route = route,
            AddressLine1 = $"{number} {route}",
            SubLocality = suburb,
            Locality = "Cape Town",
            AdministrativeAreaLevel1 = "Western Cape",
            AdministrativeAreaLevel2 = "City of Cape Town",
            Country = "South Africa",
            CountryCode = "ZA",
            PostalCode = postal,
            Latitude = lat,
            Longitude = lng,
            FormattedAddress = $"{number} {route}, {suburb}, Cape Town, {postal}, South Africa"
        };

        // Admin
        db.Users.Add(NewUser(UserRole.Admin, "EDry Admin", "admin@edrycleaners.co.za", "+27215550100"));

        // Categories
        var dryCleaning = new Category { Name = "Dry Cleaning", Description = "Clothes and garments", DisplayOrder = 1 };
        var shoeCleaning = new Category { Name = "Shoe Cleaning", Description = "Sneakers, leather shoes and boots", DisplayOrder = 2 };
        db.Categories.AddRange(dryCleaning, shoeCleaning);

        // Provider
        var providerUser = NewUser(UserRole.Provider, "Thandi Mokoena", "thandi@sparkledrycleaners.co.za", "+27825550142");
        var provider = new Provider
        {
            User = providerUser,
            ApprovedAt = DateTime.UtcNow,
            Address = NewAddress("45", "Bree Street", "Cape Town City Centre", "8001", -33.9200000m, 18.4180000m),
            BusinessName = "Sparkle Dry Cleaners",
            Description = "Premium garment and shoe care in the heart of Cape Town since 2012.",
            Phone = "+27215550199",
            Email = "hello@sparkledrycleaners.co.za",
            IsActive = true
        };
        db.ServiceProviders.Add(provider);
        db.ServiceProviderCategories.AddRange(
            new ServiceProviderCategory { ServiceProvider = provider, Category = dryCleaning },
            new ServiceProviderCategory { ServiceProvider = provider, Category = shoeCleaning });

        Product AddProduct(Category category, string name, string description, int order, params (string Name, string Description, decimal Price, int Days)[] items)
        {
            var product = new Product { ServiceProvider = provider, Category = category, Name = name, Description = description, DisplayOrder = order };
            var i = 1;
            foreach (var (itemName, itemDescription, price, days) in items)
                product.ServiceItems.Add(new ServiceItem { Name = itemName, Description = itemDescription, Price = price, EstimatedDays = days, DisplayOrder = i++ });
            db.Products.Add(product);
            return product;
        }

        var shirt = AddProduct(dryCleaning, "Shirt", "Formal and casual shirts", 1,
            ("Regular Dry Clean", "Standard dry clean and press", 45m, 2), ("Premium Dry Clean", "Hand-finished with stain treatment", 70m, 2), ("Express Dry Clean", "Priority cleaning, ready next day", 95m, 1));
        var suit = AddProduct(dryCleaning, "Suit", "Two-piece and three-piece suits", 2,
            ("Regular Dry Clean", "Standard dry clean and press", 180m, 3), ("Premium Dry Clean", "Hand-finished with stain treatment", 250m, 3), ("Express Dry Clean", "Priority cleaning, ready next day", 320m, 1));
        var dress = AddProduct(dryCleaning, "Dress", "Day and evening dresses", 3,
            ("Regular Dry Clean", "Standard dry clean and press", 90m, 2), ("Premium Dry Clean", "Hand-finished with stain treatment", 130m, 2));
        var trouser = AddProduct(dryCleaning, "Trouser", "Chinos, formal trousers and slacks", 4,
            ("Regular Dry Clean", "Standard dry clean and press", 50m, 2), ("Premium Dry Clean", "Hand-finished with stain treatment", 75m, 2));
        AddProduct(dryCleaning, "Jacket", "Blazers, coats and light jackets", 5,
            ("Regular Dry Clean", "Standard dry clean and press", 120m, 3), ("Premium Dry Clean", "Hand-finished with stain treatment", 160m, 3));
        var sneakers = AddProduct(shoeCleaning, "Sneakers", "Canvas, mesh and leather sneakers", 1,
            ("Basic Clean", "Professional shoe care", 120m, 3), ("Deep Clean", "Professional shoe care", 180m, 4), ("Whitening Treatment", "Professional shoe care", 220m, 5));
        AddProduct(shoeCleaning, "Leather Shoes", "Formal and casual leather shoes", 2,
            ("Clean & Polish", "Professional shoe care", 150m, 3), ("Restoration", "Professional shoe care", 300m, 6));

        // Marketplace drivers (no ServiceProviderId)
        Driver AddDriver(string name, string email, string phone, string license, string suburb)
        {
            var driver = new Driver
            {
                User = NewUser(UserRole.Driver, name, email, phone),
                Address = NewAddress("1", "Station Road", suburb, "7925", -33.9370000m, 18.4710000m),
                LicenseNumber = license,
                IsAvailable = true,
                ApprovedAt = DateTime.UtcNow
            };
            db.Drivers.Add(driver);
            return driver;
        }

        var bongani = AddDriver("Bongani Zulu", "bongani@drivers.edrycleaners.co.za", "+27715550110", "CA 456-218", "Observatory");
        AddDriver("Riaan du Plessis", "riaan@drivers.edrycleaners.co.za", "+27725550187", "CA 902-334", "Woodstock");

        // Customers
        Customer AddCustomer(string name, string email, string phone, string number, string route, string suburb, string postal, decimal lat, decimal lng)
        {
            var customer = new Customer { User = NewUser(UserRole.Customer, name, email, phone), Address = NewAddress(number, route, suburb, postal, lat, lng) };
            db.Customers.Add(customer);
            return customer;
        }

        var sipho = AddCustomer("Sipho Ndlovu", "sipho@example.co.za", "+27825550101", "12", "Regent Road", "Sea Point", "8005", -33.9180000m, 18.3870000m);
        var ayesha = AddCustomer("Ayesha Patel", "ayesha@example.co.za", "+27835550123", "8", "Kloof Street", "Gardens", "8001", -33.9310000m, 18.4120000m);
        var johan = AddCustomer("Johan van der Merwe", "johan@example.co.za", "+27845550145", "27", "Main Road", "Claremont", "7708", -33.9800000m, 18.4650000m);

        await db.SaveChangesAsync(ct);

        // Orders across the workflow
        ServiceItem Item(Product p, int index) => p.ServiceItems.OrderBy(s => s.DisplayOrder).ElementAt(index);

        await AddOrderAsync(db, sipho, provider, paid: true, steps: [], driver: null,
            (shirt, Item(shirt, 0), 2), (suit, Item(suit, 1), 1));
        await AddOrderAsync(db, ayesha, provider, paid: true, steps: [OrderStatus.Confirmed, OrderStatus.PickupAssigned, OrderStatus.PickedUp, OrderStatus.ReceivedByProvider, OrderStatus.Processing], driver: bongani,
            (dress, Item(dress, 1), 2), (sneakers, Item(sneakers, 1), 1));
        await AddOrderAsync(db, johan, provider, paid: true,
            steps: [OrderStatus.Confirmed, OrderStatus.PickupAssigned, OrderStatus.PickedUp, OrderStatus.ReceivedByProvider, OrderStatus.Processing, OrderStatus.QualityCheck, OrderStatus.ReadyForDelivery],
            driver: bongani, (trouser, Item(trouser, 0), 3), (shirt, Item(shirt, 0), 3));

        await db.SaveChangesAsync(ct);
    }

    private static async Task AddOrderAsync(AppDbContext db, Customer customer, Provider provider, bool paid, OrderStatus[] steps, Driver? driver,
        params (Product Product, ServiceItem ServiceItem, int Quantity)[] lines)
    {
        var order = new Order
        {
            OrderNumber = await db.NextOrderNumberAsync(CancellationToken.None),
            CustomerId = customer.Id,
            ServiceProviderId = provider.Id,
            PickupAddressId = customer.AddressId!.Value,
            PickupDate = DateTime.UtcNow.Date.AddDays(steps.Length == 0 ? 1 : -1),
            PickupSlot = "09:00 AM – 11:00 AM"
        };
        foreach (var (product, serviceItem, quantity) in lines)
        {
            order.Items.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = product.Id,
                ServiceItemId = serviceItem.Id,
                Quantity = quantity,
                UnitPrice = serviceItem.Price,
                TotalPrice = serviceItem.Price * quantity
            });
        }

        order.Reprice(45m, [], new VatSetting());
        order.MarkCreated(customer.UserId);
        order.Verifications.Add(new OrderVerification { OrderId = order.Id, Type = VerificationType.QR, OtpCode = Code() });
        order.Verifications.Add(new OrderVerification { OrderId = order.Id, Type = VerificationType.OTP, OtpCode = Code() });

        if (paid)
        {
            order.Payments.Add(new Payment
            {
                OrderId = order.Id,
                Amount = order.TotalAmount,
                Status = PaymentStatus.Success,
                Method = PaymentMethod.Card,
                TransactionId = $"{order.OrderNumber}-seed",
                PaidAt = DateTime.UtcNow
            });
        }

        foreach (var step in steps)
            order.ChangeStatus(step, provider.UserId, "Seed data");

        if (driver is not null && steps.Contains(OrderStatus.PickupAssigned))
        {
            var pickupDone = steps.Contains(OrderStatus.ReceivedByProvider);
            order.DriverJobs.Add(new DriverJob
            {
                OrderId = order.Id,
                DriverId = driver.Id,
                JobType = DriverJobType.Pickup,
                Status = pickupDone ? DriverJobStatus.Completed : DriverJobStatus.Assigned,
                PickupAddressId = order.PickupAddressId,
                DeliveryAddressId = provider.AddressId!.Value,
                Earning = 35m,
                AssignedAt = DateTime.UtcNow,
                CompletedAt = pickupDone ? DateTime.UtcNow : null
            });
        }

        db.Orders.Add(order);
    }

    private static string Code() => RandomNumberGenerator.GetInt32(0, 10000).ToString("0000");
}
