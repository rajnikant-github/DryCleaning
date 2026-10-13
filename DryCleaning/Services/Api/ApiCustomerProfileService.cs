using DryCleaning.Models;
using DryCleaning.Services.Api.Contracts;

namespace DryCleaning.Services.Api;

/// <summary>Customer profile (api/auth/me) and the one saved address (api/me/address).</summary>
public sealed class ApiCustomerProfileService(ApiClient api, ISessionService session) : ICustomerProfileService
{
    private static ApiJsonContext Json => ApiJsonContext.Default;

    public async Task<CustomerProfile> GetProfileAsync()
    {
        var userTask = api.GetAsync("api/auth/me", Json.UserDto);
        var addressTask = api.GetOrDefaultAsync("api/me/address", Json.AddressDto);
        var countsTask = api.GetAsync("api/customer/orders/counts", Json.CustomerOrderCountsDto);
        await Task.WhenAll(userTask, addressTask, countsTask);

        var counts = countsTask.Result;
        return new CustomerProfile
        {
            User = ApiMapping.ToUser(userTask.Result),
            Address = addressTask.Result is { } address ? ApiMapping.ToAddress(address) : null,
            OrderCount = counts.Active + counts.Completed + counts.Cancelled
        };
    }

    public async Task UpdateProfileAsync(string name, string? email, string? phone)
    {
        var user = await api.SendAsync(HttpMethod.Put, "api/auth/me",
            new UpdateProfileRequest
            {
                Name = name.Trim(),
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim()
            },
            Json.UpdateProfileRequest, Json.UserDto);
        session.SignIn(ApiMapping.ToUser(user), user.ProfileId ?? session.ProfileId);
    }

    public async Task<Address> SaveAddressAsync(Address address) =>
        ApiMapping.ToAddress(await api.SendAsync(HttpMethod.Put, "api/me/address", ApiMapping.ToUpsert(address), Json.UpsertAddressRequest, Json.AddressDto));
}
