using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Infrastructure.Auth;
using DryCleaning.Api.Infrastructure.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Services;

/// <summary>
/// Sign-up with email verification. <see cref="StartAsync"/> only stores a pending sign-up and emails a code;
/// the account is created by <see cref="ConfirmAsync"/> once the right code is entered.
/// </summary>
public sealed class RegistrationService(AppDbContext db, AuthService auth, IPasswordHasher hasher, EmailQueue email, IOptions<OtpOptions> otpOptions)
{
    private const int CodeMinutes = 10;
    private const int ResendCooldownSeconds = 60;
    private const int MaxSends = 5;

    public async Task<RegisterStartResponse> StartAsync(RegisterStartRequest request, CancellationToken ct)
    {
        var address = request.Email.Trim().ToLowerInvariant();
        var phone = string.IsNullOrWhiteSpace(request.Phone) ? null : AuthService.Normalize(request.Phone);
        await auth.EnsureAvailableAsync(address, phone, ct);

        var now = DateTime.UtcNow;
        await db.PendingRegistrations.Where(p => p.ExpiresAt < now).ExecuteDeleteAsync(ct); // housekeeping

        var pending = await db.PendingRegistrations.FirstOrDefaultAsync(p => p.Email == address, ct);
        if (pending is null)
        {
            pending = new PendingRegistration { Email = address };
            db.PendingRegistrations.Add(pending);
        }
        else
        {
            EnsureCanSend(pending);
        }

        pending.Role = request.Role;
        pending.Name = request.Name.Trim();
        pending.Phone = phone;
        pending.PasswordHash = hasher.Hash(request.Password);
        pending.DetailsJson = JsonSerializer.Serialize(new RegistrationDetails
        {
            BusinessName = request.BusinessName?.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CategoryIds = request.CategoryIds?.Distinct().ToList() ?? [],
            LicenseNumber = request.LicenseNumber?.Trim()
        });

        return await SendCodeAsync(pending, ct);
    }

    public async Task<RegisterStartResponse> ResendAsync(RegisterResendRequest request, CancellationToken ct)
    {
        var pending = await FindAsync(request.Email, ct);
        EnsureCanSend(pending);
        return await SendCodeAsync(pending, ct);
    }

    public async Task<AuthResponse> ConfirmAsync(RegisterConfirmRequest request, CancellationToken ct)
    {
        var pending = await FindAsync(request.Email, ct);

        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(pending.CodeHash), Encoding.UTF8.GetBytes(Hash(pending, request.Code.Trim()))))
        {
            pending.Attempts++;
            if (pending.Attempts >= otpOptions.Value.MaxAttempts)
            {
                db.PendingRegistrations.Remove(pending);
                await db.SaveChangesAsync(ct);
                throw new BadRequestException("Too many wrong codes. Please start your sign-up again.");
            }

            await db.SaveChangesAsync(ct);
            throw new UnauthorizedException("That code is incorrect or has expired.");
        }

        db.PendingRegistrations.Remove(pending); // saved together with the new account
        return await auth.CreateAccountAsync(pending, ct);
    }

    private async Task<PendingRegistration> FindAsync(string emailAddress, CancellationToken ct)
    {
        var address = emailAddress.Trim().ToLowerInvariant();
        var pending = await db.PendingRegistrations.FirstOrDefaultAsync(p => p.Email == address, ct)
                      ?? throw new NotFoundException("We couldn't find that sign-up. Please start again.");
        if (pending.ExpiresAt < DateTime.UtcNow)
        {
            db.PendingRegistrations.Remove(pending);
            await db.SaveChangesAsync(ct);
            throw new BadRequestException("That code has expired. Please start your sign-up again.");
        }

        return pending;
    }

    private static void EnsureCanSend(PendingRegistration pending)
    {
        var wait = (int)Math.Ceiling((pending.LastSentAt.AddSeconds(ResendCooldownSeconds) - DateTime.UtcNow).TotalSeconds);
        if (wait > 0)
            throw new BadRequestException($"Please wait {wait} seconds before asking for another code.");
        if (pending.SendCount >= MaxSends && pending.ExpiresAt > DateTime.UtcNow)
            throw new BadRequestException("Too many codes were requested for this email. Please try again in a few minutes.");
    }

    private async Task<RegisterStartResponse> SendCodeAsync(PendingRegistration pending, CancellationToken ct)
    {
        var settings = otpOptions.Value;
        var code = RandomNumberGenerator.GetInt32(0, (int)Math.Pow(10, settings.Length)).ToString(new string('0', settings.Length));

        pending.CodeHash = Hash(pending, code);
        pending.Attempts = 0;
        pending.SendCount = pending.ExpiresAt > DateTime.UtcNow ? pending.SendCount + 1 : 1;
        pending.LastSentAt = DateTime.UtcNow;
        pending.ExpiresAt = DateTime.UtcNow.AddMinutes(CodeMinutes);
        pending.Touch();
        await db.SaveChangesAsync(ct);

        email.Enqueue(EmailTemplates.SignUpCode(pending.Email, pending.Name, code, CodeMinutes));
        return new RegisterStartResponse
        {
            Destination = AuthService.MaskEmail(pending.Email),
            ExpiresInSeconds = CodeMinutes * 60,
            ResendInSeconds = ResendCooldownSeconds
        };
    }

    private static string Hash(PendingRegistration pending, string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{pending.Id}:{code}")));
}
