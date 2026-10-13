using DryCleaning.Configuration;
using DryCleaning.Helpers;
using DryCleaning.Models;

namespace DryCleaning.Services.Mock;

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
