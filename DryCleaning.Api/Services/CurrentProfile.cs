using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Services;

/// <summary>Resolves the signed-in user's role profile (Customer / Provider / Driver).</summary>
public sealed class CurrentProfile(AppDbContext db, ICurrentUser currentUser)
{
    public Guid UserId => currentUser.UserId ?? throw new UnauthorizedException("You need to sign in.");

    public async Task<Customer> CustomerAsync(CancellationToken ct)
    {
        var userId = UserId;
        return await db.Customers.Include(c => c.Address).FirstOrDefaultAsync(c => c.UserId == userId, ct)
               ?? throw new ForbiddenException("A customer profile is required.");
    }

    public async Task<Provider> ProviderAsync(CancellationToken ct)
    {
        var userId = UserId;
        return await db.ServiceProviders.Include(p => p.Address).FirstOrDefaultAsync(p => p.UserId == userId, ct)
               ?? throw new ForbiddenException("A service provider profile is required.");
    }

    public async Task<Driver> DriverAsync(CancellationToken ct)
    {
        var userId = UserId;
        return await db.Drivers.Include(d => d.Address).FirstOrDefaultAsync(d => d.UserId == userId, ct)
               ?? throw new ForbiddenException("A driver profile is required.");
    }
}
