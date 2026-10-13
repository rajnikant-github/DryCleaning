using DryCleaning.Models;

namespace DryCleaning.Services;

public interface ICustomerProfileService
{
    Task<CustomerProfile> GetProfileAsync();
    Task UpdateProfileAsync(string name, string? email, string? phone);
    Task<Address> SaveAddressAsync(Address address);
}
