using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Infrastructure.Auth;
using DryCleaning.Api.Infrastructure.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Services;

public sealed class AuthService(
    AppDbContext db,
    IPasswordHasher hasher,
    ITokenService tokens,
    IOtpService otp,
    EmailQueue email,
    CurrentProfile profile,
    IOptions<OtpOptions> otpOptions,
    IOptions<JwtOptions> jwtOptions,
    ILogger<AuthService> logger)
{
    /// <summary>Creates the real account (and its customer, provider or driver profile) from a confirmed sign-up.</summary>
    public async Task<AuthResponse> CreateAccountAsync(PendingRegistration pending, CancellationToken ct)
    {
        await EnsureUniqueAsync(pending.Email, pending.Phone, null, ct);

        var details = System.Text.Json.JsonSerializer.Deserialize<RegistrationDetails>(pending.DetailsJson) ?? new RegistrationDetails();
        var user = new User { Role = pending.Role, Name = pending.Name, Email = pending.Email, Phone = pending.Phone, PasswordHash = pending.PasswordHash };
        db.Users.Add(user);

        Guid profileId;
        switch (pending.Role)
        {
            case UserRole.Customer:
                var customer = new Customer { UserId = user.Id };
                db.Customers.Add(customer);
                profileId = customer.Id;
                break;

            case UserRole.Provider:
                var provider = new Provider
                {
                    UserId = user.Id,
                    BusinessName = details.BusinessName ?? string.Empty,
                    Description = details.Description,
                    Email = pending.Email,
                    Phone = pending.Phone,
                    IsActive = false // admin approval required
                };
                db.ServiceProviders.Add(provider);
                profileId = provider.Id;

                if (details.CategoryIds.Count > 0)
                {
                    var validIds = await db.Categories.Where(c => details.CategoryIds.Contains(c.Id) && c.IsActive).Select(c => c.Id).ToListAsync(ct);
                    foreach (var categoryId in validIds)
                        db.ServiceProviderCategories.Add(new ServiceProviderCategory { ServiceProviderId = provider.Id, CategoryId = categoryId });
                }

                foreach (var adminId in await db.Users.Where(u => u.Role == UserRole.Admin && u.IsActive).Select(u => u.Id).ToListAsync(ct))
                    db.Notify(adminId, null, "New provider registration", $"{provider.BusinessName} is waiting for approval.", NotificationType.System);
                break;

            default:
                var driver = new Driver { UserId = user.Id, LicenseNumber = details.LicenseNumber ?? string.Empty, IsAvailable = false };
                db.Drivers.Add(driver);
                profileId = driver.Id;
                break;
        }

        await db.SaveChangesAsync(ct);
        QueueWelcomeEmail(user);
        return await IssueAsync(user, profileId, ct);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await FindByIdentifierAsync(request.Identifier, ct);
        if (user is null || !hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Incorrect mobile number/email or password.");
        if (!user.IsActive)
            throw new ForbiddenException("This account has been deactivated.");

        return await IssueAsync(user, await ProfileIdAsync(user, ct), ct);
    }

    public async Task<RequestOtpResponse> RequestOtpAsync(RequestOtpRequest request, CancellationToken ct)
    {
        var user = await FindByIdentifierAsync(request.Identifier, ct)
                   ?? throw new NotFoundException("No account found for that mobile number or email.");

        // Codes are only ever emailed (no SMS), even when the account was found by its mobile number.
        if (string.IsNullOrWhiteSpace(user.Email))
            throw new BadRequestException("This account has no email address, so we can't send a code. Sign in with your password, then add your email in your profile.");

        var destination = user.Email;
        var code = await otp.SendAsync(Normalize(request.Identifier), destination, ct);
        QueueCodeEmail(user, code, passwordReset: false);
        var options = otpOptions.Value;
        return new RequestOtpResponse
        {
            Destination = Mask(destination),
            ExpiresInSeconds = options.ExpirySeconds,
            DevCode = options.ExposeCodeInResponse ? code : null
        };
    }

    public async Task<AuthResponse> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct)
    {
        if (!otp.Verify(Normalize(request.Identifier), request.Code))
            throw new UnauthorizedException("That code is incorrect or has expired.");

        var user = await FindByIdentifierAsync(request.Identifier, ct)
                   ?? throw new NotFoundException("Account not found.");
        if (!user.IsActive)
            throw new ForbiddenException("This account has been deactivated.");

        return await IssueAsync(user, await ProfileIdAsync(user, ct), ct);
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
        await RevokeAllRefreshTokensAsync(user.Id, ct); // sign out other devices
        await db.SaveChangesAsync(ct);
    }

    // ── Refresh tokens ───────────────────────────────────────────────────────

    /// <summary>Exchanges a refresh token for a new access token + refresh token (rotation).</summary>
    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct)
    {
        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var stored = await db.RefreshTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.TokenHash == hash, ct)
                     ?? throw new UnauthorizedException("Your session has expired. Please sign in again.");

        if (stored.RevokedAt is not null)
        {
            // A rotated token was used again – it may have been stolen. End every session for this user.
            logger.LogWarning("Refresh token reuse detected for user {UserId}; revoking all sessions", stored.UserId);
            await RevokeAllRefreshTokensAsync(stored.UserId, ct);
            await db.SaveChangesAsync(ct);
            throw new UnauthorizedException("Your session has expired. Please sign in again.");
        }
        if (stored.ExpiresAt <= DateTime.UtcNow)
            throw new UnauthorizedException("Your session has expired. Please sign in again.");
        if (!stored.User.IsActive)
            throw new ForbiddenException("This account has been deactivated.");

        var response = await IssueAsync(stored.User, await ProfileIdAsync(stored.User, ct), ct, save: false);
        stored.RevokedAt = DateTime.UtcNow;
        stored.ReplacedByTokenHash = tokens.HashRefreshToken(response.RefreshToken!);
        await db.SaveChangesAsync(ct);
        return response;
    }

    /// <summary>Signs this device out (revokes its refresh token). Unknown tokens are ignored.</summary>
    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken ct)
    {
        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash && t.RevokedAt == null, ct);
        if (stored is null)
            return;
        stored.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    // ── Forgot / reset password ──────────────────────────────────────────────

    /// <summary>
    /// Sends a reset code. Always answers the same way so it can't be used to discover accounts.
    /// </summary>
    public async Task<RequestOtpResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct)
    {
        var options = otpOptions.Value;
        var user = await FindByIdentifierAsync(request.Identifier, ct);
        if (user is null || !user.IsActive || string.IsNullOrWhiteSpace(user.Email))
            return new RequestOtpResponse
            {
                Destination = "your registered email",
                ExpiresInSeconds = options.ExpirySeconds,
                DevCode = null
            };

        var destination = user.Email;
        var code = await otp.SendAsync(ResetKey(request.Identifier), destination, ct);
        QueueCodeEmail(user, code, passwordReset: true);
        return new RequestOtpResponse
        {
            Destination = Mask(destination),
            ExpiresInSeconds = options.ExpirySeconds,
            DevCode = options.ExposeCodeInResponse ? code : null
        };
    }

    /// <summary>Checks the reset code, sets the new password, signs out other devices and signs this one in.</summary>
    public async Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct)
    {
        if (!otp.Verify(ResetKey(request.Identifier), request.Code))
            throw new BadRequestException("That code is incorrect or has expired. Request a new one.");

        var user = await FindByIdentifierAsync(request.Identifier, ct)
                   ?? throw new BadRequestException("That code is incorrect or has expired. Request a new one.");
        if (!user.IsActive)
            throw new ForbiddenException("This account has been deactivated.");

        user.PasswordHash = hasher.Hash(request.NewPassword);
        await RevokeAllRefreshTokensAsync(user.Id, ct);
        db.Notify(user.Id, null, "Password changed", "Your E Dry Cleaners password was reset. If this wasn't you, contact support.", NotificationType.System);
        await db.SaveChangesAsync(ct);
        return await IssueAsync(user, await ProfileIdAsync(user, ct), ct);
    }

    // ── Token issuing ────────────────────────────────────────────────────────

    /// <summary>Access token + a new refresh token (stored hashed).</summary>
    private async Task<AuthResponse> IssueAsync(User user, Guid? profileId, CancellationToken ct, bool save = true)
    {
        var access = tokens.Create(user, profileId);
        var (refresh, hash) = tokens.NewRefreshToken();
        var expires = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays);
        db.RefreshTokens.Add(new RefreshToken { UserId = user.Id, TokenHash = hash, ExpiresAt = expires });

        // Housekeeping: drop this user's tokens that expired more than a week ago.
        var cutoff = DateTime.UtcNow.AddDays(-7);
        db.RefreshTokens.RemoveRange(await db.RefreshTokens.Where(t => t.UserId == user.Id && t.ExpiresAt < cutoff).ToListAsync(ct));

        if (save)
            await db.SaveChangesAsync(ct);
        access.RefreshToken = refresh;
        access.RefreshTokenExpiresAt = expires;
        return access;
    }

    private async Task RevokeAllRefreshTokensAsync(Guid userId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        foreach (var token in await db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null).ToListAsync(ct))
            token.RevokedAt = now;
    }

    private static string ResetKey(string identifier) => $"reset:{Normalize(identifier)}";

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void QueueWelcomeEmail(User user)
    {
        if (!string.IsNullOrWhiteSpace(user.Email))
            email.Enqueue(EmailTemplates.Welcome(user.Email, user.Name, user.Role));
    }

    private void QueueCodeEmail(User user, string code, bool passwordReset)
    {
        if (!string.IsNullOrWhiteSpace(user.Email))
            email.Enqueue(EmailTemplates.Code(user.Email, user.Name, code, Math.Max(1, otpOptions.Value.ExpirySeconds / 60), passwordReset));
    }

    /// <summary>Throws a 409 when the email or phone already belongs to an account.</summary>
    public Task EnsureAvailableAsync(string? emailAddress, string? phone, CancellationToken ct) => EnsureUniqueAsync(emailAddress, phone, null, ct);

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

    public static string MaskEmail(string destination) => Mask(destination);

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
