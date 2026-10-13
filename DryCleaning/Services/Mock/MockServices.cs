using DryCleaning.Models;

namespace DryCleaning.Services.Mock;

public sealed class MockAuthService(MockDataStore store, ISessionService session) : IAuthService
{
    /// <summary>Demo OTP accepted by the mock service.</summary>
    public const string DemoOtp = "123456";

    public async Task<AuthResult> LoginAsync(string identifier, string password, UserRole role)
    {
        await MockDataStore.SimulateLatencyAsync();

        if (string.IsNullOrWhiteSpace(identifier))
            return AuthResult.Fail("Enter your mobile number or email.");
        if (string.IsNullOrWhiteSpace(password) || password.Length < 4)
            return AuthResult.Fail("Password must be at least 4 characters.");

        return SignInAs(role);
    }

    public async Task<string> RequestOtpAsync(string identifier, UserRole role)
    {
        await MockDataStore.SimulateLatencyAsync();
        var digits = new string(identifier.Where(char.IsDigit).ToArray());
        if (identifier.Contains('@'))
        {
            var at = identifier.IndexOf('@');
            return $"{identifier[..Math.Min(2, at)]}***{identifier[at..]}";
        }

        return digits.Length >= 4 ? $"+27 ** *** {digits[^4..]}" : identifier;
    }

    public async Task<AuthResult> VerifyOtpAsync(string identifier, string code, UserRole role)
    {
        await MockDataStore.SimulateLatencyAsync();
        return code == DemoOtp
            ? SignInAs(role)
            : AuthResult.Fail("That code is incorrect. Please try again.");
    }

    public Task LogoutAsync()
    {
        session.SignOut();
        return Task.CompletedTask;
    }

    private AuthResult SignInAs(UserRole role)
    {
        switch (role)
        {
            case UserRole.Provider:
                session.SignIn(store.DemoProviderUser, store.DemoProvider.Id);
                return AuthResult.Ok();
            case UserRole.Customer:
                session.SignIn(store.DemoCustomerUser, store.DemoCustomer.Id);
                return AuthResult.Ok();
            case UserRole.Driver:
                session.SignIn(store.DemoDriverUser, store.DemoDriver.Id);
                return AuthResult.Ok();
            default:
                return AuthResult.Fail($"The {role} app is not available in this build yet.");
        }
    }
}

public sealed class MockProviderCatalogService(MockDataStore store, ISessionService session) : IProviderCatalogService
{
    private Guid ProviderId => session.ProviderId ?? store.DemoProvider.Id;

    public async Task<IReadOnlyList<Category>> GetProviderCategoriesAsync()
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var enabled = store.ServiceProviderCategories
                .Where(pc => pc.ServiceProviderId == ProviderId && pc.IsActive)
                .Select(pc => pc.CategoryId)
                .ToHashSet();

            return store.Categories
                .Where(c => c.IsActive && enabled.Contains(c.Id))
                .OrderBy(c => c.DisplayOrder)
                .ToList();
        }
    }

    public async Task<IReadOnlyList<ProductSummary>> GetProductsAsync(Guid categoryId)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            return store.Products
                .Where(p => p.ServiceProviderId == ProviderId && p.CategoryId == categoryId)
                .OrderBy(p => p.DisplayOrder)
                .Select(p =>
                {
                    var items = store.ServiceItems.Where(s => s.ProductId == p.Id).ToList();
                    var active = items.Where(s => s.IsActive).ToList();
                    return new ProductSummary
                    {
                        Product = p,
                        ServiceItemCount = items.Count,
                        StartingPrice = active.Count == 0 ? null : active.Min(s => s.Price)
                    };
                })
                .ToList();
        }
    }

    public async Task<Product> GetProductAsync(Guid productId)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
            return store.Products.FirstOrDefault(p => p.Id == productId) ?? throw new KeyNotFoundException("Product not found.");
    }

    public async Task<Product> SaveProductAsync(Product product)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var existing = store.Products.FirstOrDefault(p => p.Id == product.Id);
            if (existing is null)
            {
                product.ServiceProviderId = ProviderId;
                product.DisplayOrder = store.Products.Count(p => p.CategoryId == product.CategoryId && p.ServiceProviderId == ProviderId) + 1;
                product.CreatedAt = product.UpdatedAt = DateTime.Now;
                store.Products.Add(product);
                return product;
            }

            existing.Name = product.Name;
            existing.Description = product.Description;
            existing.CategoryId = product.CategoryId;
            existing.IsActive = product.IsActive;
            existing.UpdatedAt = DateTime.Now;
            return existing;
        }
    }

    public async Task SetProductActiveAsync(Guid productId, bool isActive)
    {
        await Task.Delay(150);
        lock (store.Gate)
        {
            var product = store.Products.First(p => p.Id == productId);
            product.IsActive = isActive;
            product.UpdatedAt = DateTime.Now;
        }
    }

    public async Task<IReadOnlyList<ServiceItem>> GetServiceItemsAsync(Guid productId)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
            return store.ServiceItems.Where(s => s.ProductId == productId).OrderBy(s => s.DisplayOrder).ToList();
    }

    public async Task<ServiceItem> GetServiceItemAsync(Guid serviceItemId)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
            return store.ServiceItems.FirstOrDefault(s => s.Id == serviceItemId) ?? throw new KeyNotFoundException("Service item not found.");
    }

    public async Task<ServiceItem> SaveServiceItemAsync(ServiceItem serviceItem)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var existing = store.ServiceItems.FirstOrDefault(s => s.Id == serviceItem.Id);
            if (existing is null)
            {
                serviceItem.DisplayOrder = store.ServiceItems.Count(s => s.ProductId == serviceItem.ProductId) + 1;
                serviceItem.CreatedAt = serviceItem.UpdatedAt = DateTime.Now;
                store.ServiceItems.Add(serviceItem);
                return serviceItem;
            }

            existing.Name = serviceItem.Name;
            existing.Description = serviceItem.Description;
            existing.Price = serviceItem.Price;
            existing.EstimatedDays = serviceItem.EstimatedDays;
            existing.IsActive = serviceItem.IsActive;
            existing.UpdatedAt = DateTime.Now;
            return existing;
        }
    }

    public async Task SetServiceItemActiveAsync(Guid serviceItemId, bool isActive)
    {
        await Task.Delay(150);
        lock (store.Gate)
        {
            var item = store.ServiceItems.First(s => s.Id == serviceItemId);
            item.IsActive = isActive;
            item.UpdatedAt = DateTime.Now;
        }
    }
}

public sealed class MockProviderProfileService(MockDataStore store, ISessionService session) : IProviderProfileService
{
    public async Task<ProviderProfile> GetProfileAsync()
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var provider = store.Providers.First(p => p.Id == (session.ProviderId ?? store.DemoProvider.Id));
            var categoryIds = store.ServiceProviderCategories
                .Where(pc => pc.ServiceProviderId == provider.Id && pc.IsActive)
                .Select(pc => pc.CategoryId)
                .ToHashSet();

            return new ProviderProfile
            {
                Provider = provider,
                Owner = store.UserById(provider.UserId),
                Address = store.AddressById(provider.AddressId),
                Categories = store.Categories.Where(c => categoryIds.Contains(c.Id)).OrderBy(c => c.DisplayOrder).ToList()
            };
        }
    }
}

public sealed class MockNotificationService(MockDataStore store, ISessionService session) : INotificationService
{
    private Guid UserId => session.CurrentUser?.Id ?? store.DemoProvider.UserId;

    public async Task<IReadOnlyList<Notification>> GetNotificationsAsync()
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
            return store.Notifications.Where(n => n.UserId == UserId).OrderByDescending(n => n.CreatedAt).ToList();
    }

    public Task<int> GetUnreadCountAsync()
    {
        lock (store.Gate)
            return Task.FromResult(store.Notifications.Count(n => n.UserId == UserId && !n.IsRead));
    }

    public Task MarkAsReadAsync(Guid notificationId)
    {
        lock (store.Gate)
        {
            var notification = store.Notifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification is not null)
                notification.IsRead = true;
        }

        return Task.CompletedTask;
    }

    public Task MarkAllAsReadAsync()
    {
        lock (store.Gate)
        {
            foreach (var notification in store.Notifications.Where(n => n.UserId == UserId))
                notification.IsRead = true;
        }

        return Task.CompletedTask;
    }
}
