using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DryCleaning.Api.Common;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DryCleaning.Api.Infrastructure.Auth;

/// <summary>
/// In-memory OTP store. Codes are delivered by email only (the callers queue the email); there is no SMS.
/// </summary>
public sealed class MemoryOtpService(IMemoryCache cache, IOptions<OtpOptions> options, ILogger<MemoryOtpService> logger) : IOtpService
{
    public Task<string> SendAsync(string key, string destination, CancellationToken ct)
    {
        var settings = options.Value;
        var max = (int)Math.Pow(10, settings.Length);
        var code = RandomNumberGenerator.GetInt32(0, max).ToString(new string('0', settings.Length));
        cache.Set(CacheKey(key), code, TimeSpan.FromSeconds(settings.ExpirySeconds));
        cache.Remove(AttemptsKey(key));

        logger.LogInformation("OTP for {Destination}: {Code} (delivered by email)", destination, code);
        return Task.FromResult(code);
    }

    public bool Verify(string key, string code)
    {
        if (!cache.TryGetValue(CacheKey(key), out string? expected) || expected is null)
            return false;

        var ok = CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(code.Trim()));
        if (ok)
        {
            cache.Remove(CacheKey(key));
            cache.Remove(AttemptsKey(key));
            return true;
        }

        // Brute-force protection: the code is invalidated after MaxAttempts wrong guesses.
        var attempts = (cache.TryGetValue(AttemptsKey(key), out int previous) ? previous : 0) + 1;
        if (attempts >= options.Value.MaxAttempts)
        {
            cache.Remove(CacheKey(key));
            cache.Remove(AttemptsKey(key));
            logger.LogWarning("OTP for {Key} invalidated after {Attempts} wrong attempts", key, attempts);
        }
        else
        {
            cache.Set(AttemptsKey(key), attempts, TimeSpan.FromSeconds(options.Value.ExpirySeconds));
        }
        return false;
    }

    private static string CacheKey(string key) => $"otp:{key}";
    private static string AttemptsKey(string key) => $"otp-attempts:{key}";
}
