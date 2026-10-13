using DryCleaning.Models;

namespace DryCleaning.Services.Mock;

public sealed class MockProviderProfileService(MockDataStore store, ISessionService session) : IProviderProfileService
{
    private Provider CurrentProvider => store.Providers.First(p => p.Id == (session.ProviderId ?? store.DemoProvider.Id));

    public async Task<ProviderProfile> GetProfileAsync()
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var provider = CurrentProvider;
            var categoryIds = store.ServiceProviderCategories
                .Where(pc => pc.ServiceProviderId == provider.Id && pc.IsActive)
                .Select(pc => pc.CategoryId)
                .ToHashSet();

            return new ProviderProfile
            {
                Provider = provider,
                Owner = store.UserById(provider.UserId),
                Address = store.Addresses.FirstOrDefault(a => a.Id == provider.AddressId) ?? new Address(),
                Categories = store.Categories.Where(c => categoryIds.Contains(c.Id)).OrderBy(c => c.DisplayOrder).ToList()
            };
        }
    }

    public async Task UpdateProfileAsync(ProviderProfileUpdate update)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var provider = CurrentProvider;
            provider.BusinessName = update.BusinessName.Trim();
            provider.Description = update.Description?.Trim() ?? string.Empty;
            provider.Phone = update.Phone?.Trim() ?? string.Empty;
            provider.Email = update.Email?.Trim() ?? string.Empty;
            provider.LogoUrl = update.LogoUrl;
            provider.UpdatedAt = DateTime.Now;
        }
    }

    public async Task<string> UploadLogoAsync(string localPath)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            // Demo data keeps the local file path – the logo is shown straight from the phone.
            var provider = CurrentProvider;
            provider.LogoUrl = localPath;
            provider.UpdatedAt = DateTime.Now;
            return localPath;
        }
    }

    public async Task<IReadOnlyList<ProviderCategoryOption>> GetCategoryOptionsAsync()
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var providerId = CurrentProvider.Id;
            var enabled = store.ServiceProviderCategories
                .Where(pc => pc.ServiceProviderId == providerId && pc.IsActive)
                .Select(pc => pc.CategoryId)
                .ToHashSet();

            return store.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new ProviderCategoryOption
                {
                    CategoryId = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    DisplayOrder = c.DisplayOrder,
                    IsEnabled = enabled.Contains(c.Id)
                })
                .ToList();
        }
    }

    public async Task SetCategoriesAsync(IReadOnlyList<Guid> categoryIds)
    {
        await MockDataStore.SimulateLatencyAsync();
        if (categoryIds.Count == 0)
            throw new InvalidOperationException("Choose at least one category.");

        lock (store.Gate)
        {
            var providerId = CurrentProvider.Id;
            var links = store.ServiceProviderCategories.Where(pc => pc.ServiceProviderId == providerId).ToList();
            foreach (var link in links)
                link.IsActive = categoryIds.Contains(link.CategoryId);
            foreach (var categoryId in categoryIds.Where(id => links.All(l => l.CategoryId != id)))
                store.ServiceProviderCategories.Add(new ServiceProviderCategory { ServiceProviderId = providerId, CategoryId = categoryId });
        }
    }
}
