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

    public async Task<RatingsPage> GetRatingsAsync(int page, int pageSize, bool onlyWithComment)
    {
        await MockDataStore.SimulateLatencyAsync();
        return new RatingsPage(); // demo data keeps no ratings
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
            var provider = CurrentProvider;
            provider.AddressId = saved.Id;
            provider.UpdatedAt = DateTime.Now;
            return saved;
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

    public async Task<IReadOnlyList<WorkingHour>> GetWorkingHoursAsync()
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
            return store.WorkingHours.TryGetValue(CurrentProvider.Id, out var hours) ? hours.Select(Copy).ToList() : WorkingHour.DefaultWeek();
    }

    public async Task SetWorkingHoursAsync(IReadOnlyList<WorkingHour> hours)
    {
        await MockDataStore.SimulateLatencyAsync();
        if (!hours.Any(h => h.IsOpen))
            throw new InvalidOperationException("Be open on at least one day.");

        lock (store.Gate)
            store.WorkingHours[CurrentProvider.Id] = hours.Select(Copy).ToList();
    }

    private static WorkingHour Copy(WorkingHour h) => new() { Day = h.Day, IsOpen = h.IsOpen, Open = h.Open, Close = h.Close };
}
