using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Services;

/// <summary>One address per role (V1). Uses Google Places fields.</summary>
public sealed class AddressService(AppDbContext db, ICurrentUser currentUser, CurrentProfile profile)
{
    public async Task<AddressDto?> GetMineAsync(CancellationToken ct)
    {
        var address = await LoadAddressAsync(ct);
        return address?.ToDto();
    }

    public async Task<AddressDto> UpsertMineAsync(UpsertAddressRequest request, CancellationToken ct)
    {
        switch (currentUser.Role)
        {
            case UserRole.Customer:
            {
                var customer = await profile.CustomerAsync(ct);
                customer.Address = Upsert(customer.Address, request);
                break;
            }
            case UserRole.Provider:
            {
                var provider = await profile.ProviderAsync(ct);
                provider.Address = Upsert(provider.Address, request);
                break;
            }
            case UserRole.Driver:
            {
                var driver = await profile.DriverAsync(ct);
                driver.Address = Upsert(driver.Address, request);
                break;
            }
            default:
                throw new ForbiddenException("Admins don't have an address.");
        }

        await db.SaveChangesAsync(ct);
        return (await LoadAddressAsync(ct))!.ToDto();
    }

    private Address Upsert(Address? existing, UpsertAddressRequest request)
    {
        // Addresses referenced by past orders/jobs are kept unchanged – a new row is created instead.
        var address = existing is not null && !IsReferenced(existing.Id) ? existing : new Address();
        address.Apply(request);
        if (address != existing)
            db.Addresses.Add(address);
        return address;
    }

    private bool IsReferenced(Guid addressId) =>
        db.Orders.Any(o => o.PickupAddressId == addressId) ||
        db.DriverJobs.Any(j => j.PickupAddressId == addressId || j.DeliveryAddressId == addressId);

    private async Task<Address?> LoadAddressAsync(CancellationToken ct) => currentUser.Role switch
    {
        UserRole.Customer => (await profile.CustomerAsync(ct)).Address,
        UserRole.Provider => (await profile.ProviderAsync(ct)).Address,
        UserRole.Driver => (await profile.DriverAsync(ct)).Address,
        _ => null
    };
}
