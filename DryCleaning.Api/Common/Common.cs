using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Common;

// ── Exceptions (mapped to ProblemDetails by GlobalExceptionHandler) ──────────

public abstract class AppException(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class NotFoundException(string message) : AppException(message, StatusCodes.Status404NotFound);

public sealed class ForbiddenException(string message) : AppException(message, StatusCodes.Status403Forbidden);

public sealed class BadRequestException(string message) : AppException(message, StatusCodes.Status400BadRequest);

public sealed class ConflictException(string message) : AppException(message, StatusCodes.Status409Conflict);

public sealed class UnauthorizedException(string message) : AppException(message, StatusCodes.Status401Unauthorized);

// ── Current user ─────────────────────────────────────────────────────────────

public interface ICurrentUser
{
    Guid? UserId { get; }
    UserRole? Role { get; }
    bool IsAuthenticated { get; }
}

public static class Roles
{
    public const string Customer = nameof(UserRole.Customer);
    public const string Provider = nameof(UserRole.Provider);
    public const string Driver = nameof(UserRole.Driver);
    public const string Admin = nameof(UserRole.Admin);
}

// ── Options (appsettings.json) ───────────────────────────────────────────────

/// <summary>Section "Marketplace": currency, VAT and fees. Change values in appsettings.json.</summary>
public sealed class MarketplaceOptions
{
    public const string Section = "Marketplace";

    public string Currency { get; set; } = "ZAR";
    public decimal VatRate { get; set; } = 0.15m;
    public decimal DeliveryFee { get; set; } = 45m;
    public decimal DriverEarningPerJob { get; set; } = 35m;

    /// <summary>When true, providers only see/accept orders with a successful payment.</summary>
    public bool RequirePaymentBeforeAccept { get; set; } = true;
}

public sealed class JwtOptions
{
    public const string Section = "Jwt";

    public string Issuer { get; set; } = "EDryCleaners";
    public string Audience { get; set; } = "EDryCleaners.App";
    public string SigningKey { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60 * 24 * 7;
}

public sealed class PaystackOptions
{
    public const string Section = "Paystack";

    public string BaseUrl { get; set; } = "https://api.paystack.co/";
    public string SecretKey { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>Where Paystack redirects after checkout (deep link or web page).</summary>
    public string? CallbackUrl { get; set; }
}

public sealed class OtpOptions
{
    public const string Section = "Otp";

    public int Length { get; set; } = 6;
    public int ExpirySeconds { get; set; } = 300;

    /// <summary>Development only: return the code in the API response instead of sending an SMS.</summary>
    public bool ExposeCodeInResponse { get; set; }
}

public sealed class StorageOptions
{
    public const string Section = "Storage";

    /// <summary>Folder under wwwroot where uploaded order photos are saved.</summary>
    public string UploadsFolder { get; set; } = "uploads";
    public long MaxUploadBytes { get; set; } = 5 * 1024 * 1024;
}

// ── Paging ───────────────────────────────────────────────────────────────────

public class PagedRequest
{
    private int _page = 1;
    private int _pageSize = 20;

    public int Page { get => _page; set => _page = Math.Max(1, value); }
    public int PageSize { get => _pageSize; set => _pageSize = Math.Clamp(value, 1, 100); }

    /// <summary>Optional free-text search.</summary>
    public string? Search { get; set; }
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public static class PagingExtensions
{
    public static async Task<(List<T> Items, int Total)> PageAsync<T>(this IQueryable<T> query, PagedRequest request, CancellationToken ct)
    {
        var total = await query.CountAsync(ct);
        var items = await query.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(ct);
        return (items, total);
    }
}
