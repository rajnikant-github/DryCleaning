using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Services;

/// <summary>Entity → DTO mapping and common query shapes.</summary>
public static class Mapping
{
    public static UserDto ToDto(this User user, Guid? profileId) =>
        new(user.Id, user.Role, user.Name, user.Email, user.Phone, profileId, user.IsActive);

    public static AddressDto ToDto(this Address a) => new(
        a.Id, a.GooglePlaceId, a.FormattedAddress, a.AddressLine1, a.AddressLine2, a.StreetNumber, a.Route,
        a.SubLocality, a.Locality, a.AdministrativeAreaLevel1, a.AdministrativeAreaLevel2, a.Country,
        a.CountryCode, a.PostalCode, a.Latitude, a.Longitude, a.PlusCode);

    public static void Apply(this Address a, UpsertAddressRequest r)
    {
        a.GooglePlaceId = r.GooglePlaceId;
        a.FormattedAddress = r.FormattedAddress.Trim();
        a.AddressLine1 = r.AddressLine1;
        a.AddressLine2 = r.AddressLine2;
        a.StreetNumber = r.StreetNumber;
        a.Route = r.Route;
        a.SubLocality = r.SubLocality;
        a.Locality = r.Locality;
        a.AdministrativeAreaLevel1 = r.AdministrativeAreaLevel1;
        a.AdministrativeAreaLevel2 = r.AdministrativeAreaLevel2;
        a.Country = r.Country;
        a.CountryCode = r.CountryCode;
        a.PostalCode = r.PostalCode;
        a.Latitude = r.Latitude;
        a.Longitude = r.Longitude;
        a.PlusCode = r.PlusCode;
    }

    public static CategoryDto ToDto(this Category c) => new(c.Id, c.Name, c.Description, c.ImageUrl, c.DisplayOrder, c.IsActive);

    public static ServiceItemDto ToDto(this ServiceItem s) =>
        new(s.Id, s.ProductId, s.Name, s.Description, s.ImageUrl, s.Price, s.EstimatedDays, s.DisplayOrder, s.IsActive);

    public static ProductDto ToDto(this Product p, bool activeItemsOnly = false)
    {
        var items = p.ServiceItems
            .Where(s => !activeItemsOnly || s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .Select(s => s.ToDto())
            .ToList();
        var active = p.ServiceItems.Where(s => s.IsActive).ToList();
        return new ProductDto(p.Id, p.CategoryId, p.Category?.Name ?? string.Empty, p.Name, p.Description, p.ImageUrl,
            p.DisplayOrder, p.IsActive, active.Count == 0 ? null : active.Min(s => s.Price), items);
    }

    public static NotificationDto ToDto(this Notification n) =>
        new(n.Id, n.OrderId, n.Title, n.Message, n.Type, n.IsRead, n.CreatedAt);

    public static PaymentDto ToDto(this Payment p) =>
        new(p.Id, p.Amount, p.Status, p.Method, p.TransactionId, p.PaidAt, p.CreatedAt);

    public static OrderPhotoDto ToDto(this OrderPhoto p) => new(p.Id, p.Type, p.Url, p.UploadedBy, p.CreatedAt);

    public static DisputeDto ToDto(this Dispute d) =>
        new(d.Id, d.OrderId, d.Order?.OrderNumber ?? string.Empty, d.RaisedByUserId, d.RaisedByUser?.Name ?? string.Empty,
            d.Reason, d.Description, d.Status, d.Resolution, d.CreatedAt);

    // ── Orders ───────────────────────────────────────────────────────────────

    /// <summary>Includes needed for <see cref="ToSummary"/>.</summary>
    public static IQueryable<Order> WithSummaryIncludes(this IQueryable<Order> query) => query
        .Include(o => o.Customer).ThenInclude(c => c.User)
        .Include(o => o.ServiceProvider)
        .Include(o => o.Items).ThenInclude(i => i.Product)
        .Include(o => o.Payments)
        .Include(o => o.Disputes)
        .AsSplitQuery();

    /// <summary>Includes needed for <see cref="ToDetail"/>.</summary>
    public static IQueryable<Order> WithDetailIncludes(this IQueryable<Order> query) => query
        .Include(o => o.Customer).ThenInclude(c => c.User)
        .Include(o => o.ServiceProvider).ThenInclude(p => p.Address)
        .Include(o => o.PickupAddress)
        .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Category)
        .Include(o => o.Items).ThenInclude(i => i.ServiceItem)
        .Include(o => o.StatusHistory)
        .Include(o => o.Photos)
        .Include(o => o.Payments)
        .Include(o => o.Verifications)
        .Include(o => o.Disputes).ThenInclude(d => d.RaisedByUser)
        .Include(o => o.DriverJobs).ThenInclude(j => j.Driver!).ThenInclude(d => d.User)
        .Include(o => o.DriverJobs).ThenInclude(j => j.PickupAddress)
        .Include(o => o.DriverJobs).ThenInclude(j => j.DeliveryAddress)
        .AsSplitQuery();

    public static bool IsPaid(this Order o) => o.Payments.Any(p => p.Status == PaymentStatus.Success);

    public static bool HasOpenDispute(this Order o) =>
        o.Disputes.Any(d => d.Status is DisputeStatus.Open or DisputeStatus.UnderReview);

    public static OrderSummaryDto ToSummary(this Order o) => new(
        o.Id,
        o.OrderNumber,
        o.Status,
        o.Customer.User.Name,
        o.ServiceProvider.BusinessName,
        o.Items.Sum(i => i.Quantity),
        string.Join(", ", o.Items.Select(i => $"{i.Quantity} {i.Product.Name.ToLowerInvariant()}")),
        o.TotalAmount,
        o.IsPaid(),
        o.HasOpenDispute(),
        o.PickupDate,
        o.PickupSlot,
        o.CreatedAt,
        o.UpdatedAt);

    public static OrderItemDto ToDto(this OrderItem i) => new(
        i.Id, i.ProductId, i.ServiceItemId, i.Product.Category?.Name ?? string.Empty, i.Product.Name,
        i.ServiceItem.Name, i.Quantity, i.UnitPrice, i.TotalPrice, i.Notes, i.ServiceItem.EstimatedDays);

    public static DriverJobDto ToDto(this DriverJob j, Order order, decimal earnings)
    {
        var toProvider = j.JobType == DriverJobType.Pickup;
        return new DriverJobDto(
            j.Id, j.OrderId, order.OrderNumber, j.JobType, j.Status,
            j.DriverId, j.Driver?.User?.Name, j.Driver?.User?.Phone,
            toProvider ? order.Customer.User.Name : order.ServiceProvider.BusinessName, j.PickupAddress.ToDto(),
            toProvider ? order.ServiceProvider.BusinessName : order.Customer.User.Name, j.DeliveryAddress.ToDto(),
            order.Items.Sum(i => i.Quantity), earnings, j.AssignedAt, j.CompletedAt, j.CreatedAt);
    }

    /// <param name="includeCustomerCodes">Only the customer may see the pickup code and delivery OTP.</param>
    public static OrderDetailDto ToDetail(this Order o, string currency, decimal driverEarning, bool includeCustomerCodes) => new(
        o.Id,
        o.OrderNumber,
        o.Status,
        new PartyDto(o.Customer.Id, o.Customer.User.Name, o.Customer.User.Phone),
        new PartyDto(o.ServiceProvider.Id, o.ServiceProvider.BusinessName, o.ServiceProvider.Phone),
        o.PickupAddress.ToDto(),
        o.ServiceProvider.Address?.ToDto(),
        o.PickupDate,
        o.PickupSlot,
        o.Items.OrderBy(i => i.CreatedAt).Select(i => i.ToDto()).ToList(),
        o.SubTotal,
        o.Tax,
        o.DeliveryFee,
        o.TotalAmount,
        currency,
        o.IsPaid(),
        o.StatusHistory.OrderBy(h => h.CreatedAt).Select(h => new StatusHistoryDto(h.Status, h.Notes, h.ChangedByUserId, h.CreatedAt)).ToList(),
        o.Photos.OrderBy(p => p.CreatedAt).Select(p => p.ToDto()).ToList(),
        o.DriverJobs.OrderBy(j => j.CreatedAt).Select(j => j.ToDto(o, driverEarning)).ToList(),
        o.Payments.OrderBy(p => p.CreatedAt).Select(p => p.ToDto()).ToList(),
        o.Disputes.OrderBy(d => d.CreatedAt).Select(d => d.ToDto()).ToList(),
        includeCustomerCodes ? o.Verifications.FirstOrDefault(v => v.Type == VerificationType.QR && v.VerifiedAt == null)?.OtpCode : null,
        includeCustomerCodes ? o.Verifications.FirstOrDefault(v => v.Type == VerificationType.OTP && v.VerifiedAt == null)?.OtpCode : null,
        o.CreatedAt,
        o.UpdatedAt);

    // ── Notifications ────────────────────────────────────────────────────────

    public static void Notify(this AppDbContext db, Guid userId, Order? order, string title, string message,
        NotificationType type = NotificationType.Order)
    {
        db.Notifications.Add(new Notification
        {
            UserId = userId,
            OrderId = order?.Id,
            Title = title,
            Message = message,
            Type = type
        });
    }
}
