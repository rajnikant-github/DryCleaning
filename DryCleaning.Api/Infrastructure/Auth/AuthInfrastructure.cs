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

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);

    public bool Verify(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}

public interface ITokenService
{
    /// <summary>Creates the short-lived JWT access token (no refresh token – see AuthService.IssueAsync).</summary>
    AuthResponse Create(User user, Guid? profileId);

    /// <summary>A new random refresh token and its SHA-256 hash (only the hash is stored).</summary>
    (string Token, string Hash) NewRefreshToken();

    string HashRefreshToken(string token);
}

public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    public const string ProfileIdClaim = "profile_id";

    public AuthResponse Create(User user, Guid? profileId)
    {
        var settings = options.Value;
        var expires = DateTime.UtcNow.AddMinutes(settings.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Name, user.Name),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        if (profileId is not null)
            claims.Add(new Claim(ProfileIdClaim, profileId.Value.ToString()));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            SigningCredentials = new SigningCredentials(SigningKey(settings), SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new AuthResponse(token, expires, user.ToDto(profileId));
    }

    public (string Token, string Hash) NewRefreshToken()
    {
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(48));
        return (token, HashRefreshToken(token));
    }

    public string HashRefreshToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim())));

    public static SymmetricSecurityKey SigningKey(JwtOptions settings) =>
        new(Encoding.UTF8.GetBytes(settings.SigningKey));
}

public interface IOtpService
{
    /// <summary>Generates, stores and sends a code. Returns the code (for dev exposure only).</summary>
    Task<string> SendAsync(string key, string destination, CancellationToken ct);
    bool Verify(string key, string code);
}

/// <summary>
/// In-memory OTP store. The SMS step is logged only – plug in an SMS provider (e.g. Clickatell, Twilio)
/// in <see cref="SendAsync"/> before going live.
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

        logger.LogInformation("OTP for {Destination}: {Code} (SMS sending not configured)", destination, code);
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

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)
            ? id
            : null;

    public UserRole? Role =>
        Enum.TryParse<UserRole>(Principal?.FindFirstValue(ClaimTypes.Role), out var role) ? role : null;
}
