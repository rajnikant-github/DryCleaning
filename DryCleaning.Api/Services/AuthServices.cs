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

public sealed class AuthService(
    AppDbContext db,
    IPasswordHasher hasher,
    ITokenService tokens,
    IOtpService otp,
    CurrentProfile profile,
    IOptions<OtpOptions> otpOptions)
{
    public async Task<AuthResponse> RegisterCustomerAsync(RegisterCustomerRequest request, CancellationToken ct)
    {
        var user = await CreateUserAsync(UserRole.Customer, request.Name, request.Email, request.Phone, request.Password, ct);
        var customer = new Customer { UserId = user.Id };
        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);
        return tokens.Create(user, customer.Id);
    }

    public async Task<AuthResponse> RegisterProviderAsync(RegisterProviderRequest request, CancellationToken ct)
    {
        var user = await CreateUserAsync(UserRole.Provider, request.Name, request.Email, request.Phone, request.Password, ct);
        var provider = new Provider
        {
            UserId = user.Id,
            BusinessName = request.BusinessName.Trim(),
            Description = request.Description,
            Email = request.Email,
            Phone = request.Phone,
            IsActive = false // admin approval required
        };
        db.ServiceProviders.Add(provider);

        if (request.CategoryIds is { Count: > 0 })
        {
            var validIds = await db.Categories.Where(c => request.CategoryIds.Contains(c.Id) && c.IsActive).Select(c => c.Id).ToListAsync(ct);
            foreach (var categoryId in validIds)
                db.ServiceProviderCategories.Add(new ServiceProviderCategory { ServiceProviderId = provider.Id, CategoryId = categoryId });
        }

        foreach (var adminId in await db.Users.Where(u => u.Role == UserRole.Admin && u.IsActive).Select(u => u.Id).ToListAsync(ct))
            db.Notify(adminId, null, "New provider registration", $"{provider.BusinessName} is waiting for approval.", NotificationType.System);

        await db.SaveChangesAsync(ct);
        return tokens.Create(user, provider.Id);
    }

    public async Task<AuthResponse> RegisterDriverAsync(RegisterDriverRequest request, CancellationToken ct)
    {
        var user = await CreateUserAsync(UserRole.Driver, request.Name, request.Email, request.Phone, request.Password, ct);
        var driver = new Driver { UserId = user.Id, LicenseNumber = request.LicenseNumber.Trim(), IsAvailable = false };
        db.Drivers.Add(driver);
        await db.SaveChangesAsync(ct);
        return tokens.Create(user, driver.Id);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await FindByIdentifierAsync(request.Identifier, ct);
        if (user is null || !hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Incorrect mobile number/email or password.");
        if (!user.IsActive)
            throw new ForbiddenException("This account has been deactivated.");

        return tokens.Create(user, await ProfileIdAsync(user, ct));
    }

    public async Task<RequestOtpResponse> RequestOtpAsync(RequestOtpRequest request, CancellationToken ct)
    {
        var user = await FindByIdentifierAsync(request.Identifier, ct)
                   ?? throw new NotFoundException("No account found for that mobile number or email.");

        var destination = user.Phone ?? user.Email ?? request.Identifier;
        var code = await otp.SendAsync(Normalize(request.Identifier), destination, ct);
        var options = otpOptions.Value;
        return new RequestOtpResponse(Mask(destination), options.ExpirySeconds, options.ExposeCodeInResponse ? code : null);
    }

    public async Task<AuthResponse> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct)
    {
        if (!otp.Verify(Normalize(request.Identifier), request.Code))
            throw new UnauthorizedException("That code is incorrect or has expired.");

        var user = await FindByIdentifierAsync(request.Identifier, ct)
                   ?? throw new NotFoundException("Account not found.");
        if (!user.IsActive)
            throw new ForbiddenException("This account has been deactivated.");

        return tokens.Create(user, await ProfileIdAsync(user, ct));
    }

    public async Task<UserDto> MeAsync(CancellationToken ct)
    {
        var userId = profile.UserId;
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new NotFoundException("User not found.");
        return user.ToDto(await ProfileIdAsync(user, ct));
    }

    public async Task<UserDto> UpdateMeAsync(UpdateProfileRequest request, CancellationToken ct)
    {
        var userId = profile.UserId;
        var user = await db.Users.FirstAsync(u => u.Id == userId, ct);
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant();
        var phone = string.IsNullOrWhiteSpace(request.Phone) ? null : Normalize(request.Phone);
        await EnsureUniqueAsync(email, phone, user.Id, ct);

        user.Name = request.Name.Trim();
        user.Email = email;
        user.Phone = phone;
        await db.SaveChangesAsync(ct);
        return user.ToDto(await ProfileIdAsync(user, ct));
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = profile.UserId;
        var user = await db.Users.FirstAsync(u => u.Id == userId, ct);
        if (!hasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new BadRequestException("Current password is incorrect.");

        user.PasswordHash = hasher.Hash(request.NewPassword);
        await db.SaveChangesAsync(ct);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<User> CreateUserAsync(UserRole role, string name, string? email, string? phone, string password, CancellationToken ct)
    {
        email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        phone = string.IsNullOrWhiteSpace(phone) ? null : Normalize(phone);
        if (email is null && phone is null)
            throw new BadRequestException("A mobile number or email is required.");

        await EnsureUniqueAsync(email, phone, null, ct);

        var user = new User
        {
            Role = role,
            Name = name.Trim(),
            Email = email,
            Phone = phone,
            PasswordHash = hasher.Hash(password)
        };
        db.Users.Add(user);
        return user;
    }

    private async Task EnsureUniqueAsync(string? email, string? phone, Guid? exceptUserId, CancellationToken ct)
    {
        if (email is not null && await db.Users.AnyAsync(u => u.Email == email && u.Id != exceptUserId, ct))
            throw new ConflictException("An account with this email already exists.");
        if (phone is not null && await db.Users.AnyAsync(u => u.Phone == phone && u.Id != exceptUserId, ct))
            throw new ConflictException("An account with this mobile number already exists.");
    }

    private Task<User?> FindByIdentifierAsync(string identifier, CancellationToken ct)
    {
        var value = identifier.Trim();
        if (value.Contains('@'))
        {
            var email = value.ToLowerInvariant();
            return db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        }

        var phone = Normalize(value);
        return db.Users.FirstOrDefaultAsync(u => u.Phone == phone, ct);
    }

    private async Task<Guid?> ProfileIdAsync(User user, CancellationToken ct) => user.Role switch
    {
        UserRole.Customer => await db.Customers.Where(c => c.UserId == user.Id).Select(c => (Guid?)c.Id).FirstOrDefaultAsync(ct),
        UserRole.Provider => await db.ServiceProviders.Where(p => p.UserId == user.Id).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct),
        UserRole.Driver => await db.Drivers.Where(d => d.UserId == user.Id).Select(d => (Guid?)d.Id).FirstOrDefaultAsync(ct),
        _ => null
    };

    /// <summary>Normalizes phone numbers to digits with a leading + (e.g. +27825550142).</summary>
    public static string Normalize(string identifier)
    {
        var value = identifier.Trim();
        if (value.Contains('@'))
            return value.ToLowerInvariant();

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.StartsWith('0') && digits.Length == 10)
            digits = "27" + digits[1..]; // South African local format → international
        return "+" + digits;
    }

    private static string Mask(string destination)
    {
        if (destination.Contains('@'))
        {
            var at = destination.IndexOf('@');
            return $"{destination[..Math.Min(2, at)]}***{destination[at..]}";
        }

        return destination.Length > 4 ? $"{destination[..3]} *** {destination[^4..]}" : destination;
    }
}
