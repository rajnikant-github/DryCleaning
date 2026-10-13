using DryCleaning.Models;

namespace DryCleaning.Services.Mock;

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
