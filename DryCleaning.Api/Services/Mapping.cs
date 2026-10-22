using DryCleaning.Api.Infrastructure.Storage;
using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Services;

/// <summary>Entity → DTO mapping and common query shapes.</summary>
public static class Mapping
{
    public static UserDto ToDto(this User user, Guid? profileId, ProfileStatus profileStatus = ProfileStatus.Active) =>
        new()
        {
            Id = user.Id,
            Role = user.Role,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            ProfileId = profileId,
            ProfileStatus = profileStatus,
            IsActive = user.IsActive,
            PhotoUrl = FileUrls.Resolve(user.PhotoUrl)
        };

    public static AddressDto ToDto(this Address a) => new()
    {
        Id = a.Id,
        GooglePlaceId = a.GooglePlaceId,
        FormattedAddress = a.FormattedAddress,
        AddressLine1 = a.AddressLine1,
        AddressLine2 = a.AddressLine2,
        StreetNumber = a.StreetNumber,
        Route = a.Route,
        SubLocality = a.SubLocality,
        Locality = a.Locality,
        AdministrativeAreaLevel1 = a.AdministrativeAreaLevel1,
        AdministrativeAreaLevel2 = a.AdministrativeAreaLevel2,
        Country = a.Country,
        CountryCode = a.CountryCode,
        PostalCode = a.PostalCode,
        Latitude = a.Latitude,
        Longitude = a.Longitude,
        PlusCode = a.PlusCode
    };

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

    public static CategoryDto ToDto(this Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Description = c.Description,
        ImageUrl = c.ImageUrl,
        DisplayOrder = c.DisplayOrder,
        IsActive = c.IsActive
    };

    public static ServiceItemDto ToDto(this ServiceItem s) =>
        new()
        {
            Id = s.Id,
            ProductId = s.ProductId,
            Name = s.Name,
            Description = s.Description,
            ImageUrl = s.ImageUrl,
            Price = s.Price,
            EstimatedDays = s.EstimatedDays,
            DisplayOrder = s.DisplayOrder,
            IsActive = s.IsActive
        };

    public static ProductDto ToDto(this Product p, bool activeItemsOnly = false)
    {
        var items = p.ServiceItems
            .Where(s => !activeItemsOnly || s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .Select(s => s.ToDto())
            .ToList();
        var active = p.ServiceItems.Where(s => s.IsActive).ToList();
        return new ProductDto
        {
            Id = p.Id,
            ServiceProviderId = p.ServiceProviderId,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name ?? string.Empty,
            Name = p.Name,
            Description = p.Description,
            ImageUrl = p.ImageUrl,
            DisplayOrder = p.DisplayOrder,
            IsActive = p.IsActive,
            StartingPrice = active.Count == 0 ? null : active.Min(s => s.Price),
            ServiceItems = items
        };
    }

    public static NotificationDto ToDto(this Notification n) =>
        new()
        {
            Id = n.Id,
            OrderId = n.OrderId,
            Title = n.Title,
            Message = n.Message,
            Type = n.Type,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt
        };

    public static PaymentDto ToDto(this Payment p) =>
        new()
        {
            Id = p.Id,
            Amount = p.Amount,
            Status = p.Status,
            Method = p.Method,
            TransactionId = p.TransactionId,
            PaidAt = p.PaidAt,
            CreatedAt = p.CreatedAt
        };

    public static OrderPhotoDto ToDto(this OrderPhoto p) => new()
    {
        Id = p.Id,
        Type = p.Type,
        Url = FileUrls.Resolve(p.Url) ?? p.Url,
        UploadedBy = p.UploadedBy,
        CreatedAt = p.CreatedAt
    };

    public static DisputeDto ToDto(this Dispute d) =>
        new()
        {
            Id = d.Id,
            OrderId = d.OrderId,
            OrderNumber = d.Order?.OrderNumber ?? string.Empty,
            RaisedByUserId = d.RaisedByUserId,
            RaisedByName = d.RaisedByUser?.Name ?? string.Empty,
            Reason = d.Reason,
            Description = d.Description,
            Status = d.Status,
            Resolution = d.Resolution,
            CreatedAt = d.CreatedAt
        };

    // ── Orders ───────────────────────────────────────────────────────────────

    /// <summary>Includes needed for <see cref="ToSummary"/>.</summary>
    public static IQueryable<Order> WithSummaryIncludes(this IQueryable<Order> query) => query
        .Include(o => o.Customer).ThenInclude(c => c.User)
        .Include(o => o.ServiceProvider)
        .Include(o => o.Items).ThenInclude(i => i.Product)
        .Include(o => o.Payments)
        .Include(o => o.Disputes)
        .Include(o => o.DriverJobs)
        .AsSplitQuery();

    /// <summary>Includes needed for <see cref="ToDetail"/>.</summary>
    public static IQueryable<Order> WithDetailIncludes(this IQueryable<Order> query) => query
        .Include(o => o.Customer).ThenInclude(c => c.User)
        .Include(o => o.ServiceProvider).ThenInclude(p => p.Address)
        .Include(o => o.PickupAddress)
        .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Category)
        .Include(o => o.Items).ThenInclude(i => i.ServiceItem)
        .Include(o => o.ExtraCharges)
        .Include(o => o.StatusHistory)
        .Include(o => o.Photos)
        .Include(o => o.Payments)
        .Include(o => o.Verifications)
        .Include(o => o.Disputes).ThenInclude(d => d.RaisedByUser)
        .Include(o => o.Rating)
        .Include(o => o.DriverJobs).ThenInclude(j => j.Driver!).ThenInclude(d => d.User)
        .Include(o => o.DriverJobs).ThenInclude(j => j.PickupAddress)
        .Include(o => o.DriverJobs).ThenInclude(j => j.DeliveryAddress)
        .AsSplitQuery();

    /// <summary>The first word of the customer's name, so a provider never sees the full name in a rating.</summary>
    public static string FirstName(string? fullName) =>
        string.IsNullOrWhiteSpace(fullName) ? "Customer" : fullName.Trim().Split(' ', 2)[0];

    public static RatingDto ToDto(this Rating r, string orderNumber, string customerName) => new()
    {
        Id = r.Id,
        OrderId = r.OrderId,
        OrderNumber = orderNumber,
        Stars = r.Stars,
        Comment = r.CommentHidden ? null : r.Comment,
        CustomerName = FirstName(customerName),
        CreatedAt = r.CreatedAt
    };

    public static bool IsPaid(this Order o) => o.Payments.Any(p => p.Status == PaymentStatus.Success);

    /// <summary>The driver has dropped the picked-up items at the provider (pickup job completed). Needs <c>DriverJobs</c> loaded.</summary>
    public static bool DriverHandedOver(this Order o) =>
        o.DriverJobs.Any(j => j.JobType == DriverJobType.Pickup && j.Status == DriverJobStatus.Completed);

    public static bool HasOpenDispute(this Order o) =>
        o.Disputes.Any(d => d.Status is DisputeStatus.Open or DisputeStatus.UnderReview);

    public static OrderSummaryDto ToSummary(this Order o) => new()
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        Status = o.Status,
        CustomerName = o.Customer.User.Name,
        ProviderName = o.ServiceProvider.BusinessName,
        ItemCount = o.Items.Sum(i => i.Quantity),
        ItemsText = string.Join(", ", o.Items.Select(i => $"{i.Quantity} × {i.Product.Name.ToLowerInvariant()}")),
        TotalAmount = o.TotalAmount,
        IsPaid = o.IsPaid(),
        HasOpenDispute = o.HasOpenDispute(),
        DriverHandedOver = o.DriverHandedOver(),
        PickupDate = o.PickupDate,
        PickupSlot = o.PickupSlot,
        CreatedAt = o.CreatedAt,
        UpdatedAt = o.UpdatedAt
    };

    public static OrderItemDto ToDto(this OrderItem i) => new()
    {
        Id = i.Id,
        ProductId = i.ProductId,
        ServiceItemId = i.ServiceItemId,
        CategoryName = i.Product.Category?.Name ?? string.Empty,
        ProductName = i.Product.Name,
        ServiceItemName = i.ServiceItem.Name,
        Quantity = i.Quantity,
        UnitPrice = i.UnitPrice,
        TotalPrice = i.TotalPrice,
        Notes = i.Notes,
        EstimatedDays = i.ServiceItem.EstimatedDays
    };

    public static DriverJobDto ToDto(this DriverJob j, Order order, decimal earnings)
    {
        var toProvider = j.JobType == DriverJobType.Pickup;
        return new DriverJobDto
        {
            Id = j.Id,
            OrderId = j.OrderId,
            OrderNumber = order.OrderNumber,
            JobType = j.JobType,
            Status = j.Status,
            DriverId = j.DriverId,
            DriverName = j.Driver?.User?.Name,
            DriverPhone = j.Driver?.User?.Phone,
            DriverPhotoUrl = FileUrls.Resolve(j.Driver?.User?.PhotoUrl),
            PickupName = toProvider ? order.Customer.User.Name : order.ServiceProvider.BusinessName,
            PickupAddress = j.PickupAddress.ToDto(),
            DeliveryName = toProvider ? order.ServiceProvider.BusinessName : order.Customer.User.Name,
            DeliveryAddress = j.DeliveryAddress.ToDto(),
            ItemCount = order.Items.Sum(i => i.Quantity),
            EstimatedEarnings = earnings,
            AssignedAt = j.AssignedAt,
            CompletedAt = j.CompletedAt,
            CreatedAt = j.CreatedAt,
            OrderStatus = order.Status,
            PickupDate = order.PickupDate,
            PickupSlot = order.PickupSlot,
            DriverNotes = order.DriverNotes,
            IsOnHold = order.Status == OrderStatus.Disputed
        };
    }

    /// <summary>
    /// D03 job detail. Needs Order → Customer.User, ServiceProvider, Items → Product.Category / ServiceItem.
    /// Phone numbers are only included while the job is the caller's own and active.
    /// </summary>
    public static DriverJobDetailDto ToDetail(this DriverJob j, Guid callerDriverId, decimal earnings)
    {
        var order = j.Order;
        var isMine = j.DriverId == callerDriverId;
        var step = j.NextStep(isMine);
        var sharePhones = isMine && j.Status is DriverJobStatus.Assigned or DriverJobStatus.InProgress;

        return new DriverJobDetailDto
        {
            Job = j.ToDto(order, earnings),
            NextStep = step,
            IsMine = isMine,
            CustomerName = order.Customer.User.Name,
            CustomerPhone = sharePhones ? order.Customer.User.Phone : null,
            ProviderName = order.ServiceProvider.BusinessName,
            ProviderPhone = sharePhones ? order.ServiceProvider.Phone : null,
            Items = order.Items.OrderBy(i => i.CreatedAt).Select(i => i.ToDto()).ToList()
        };
    }

    public static DriverJobStep NextStep(this DriverJob j, bool isMine)
    {
        var orderStatus = j.Order.Status;
        if (j.Status == DriverJobStatus.Cancelled || orderStatus == OrderStatus.Cancelled)
            return DriverJobStep.Unavailable;
        if (j.Status == DriverJobStatus.Pending)
            return orderStatus == OrderStatus.Disputed ? DriverJobStep.Unavailable : DriverJobStep.Accept;
        if (!isMine)
            return DriverJobStep.Unavailable;

        return (j.JobType, j.Status) switch
        {
            (_, DriverJobStatus.Completed) => DriverJobStep.Done,
            (DriverJobType.Pickup, DriverJobStatus.Assigned) => DriverJobStep.CollectFromCustomer,
            (DriverJobType.Pickup, DriverJobStatus.InProgress) => DriverJobStep.DropAtProvider,
            (DriverJobType.Delivery, DriverJobStatus.Assigned) => DriverJobStep.CollectFromProvider,
            (DriverJobType.Delivery, DriverJobStatus.InProgress) => DriverJobStep.DeliverToCustomer,
            _ => DriverJobStep.Unavailable
        };
    }

    /// <param name="includeCustomerCodes">Only the customer may see the pickup code and delivery OTP.</param>
    public static OrderDetailDto ToDetail(this Order o, string currency, decimal driverEarning, bool includeCustomerCodes) => new()
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        Status = o.Status,
        Customer = new PartyDto { Id = o.Customer.Id, Name = o.Customer.User.Name, Phone = o.Customer.User.Phone },
        Provider = new PartyDto { Id = o.ServiceProvider.Id, Name = o.ServiceProvider.BusinessName, Phone = o.ServiceProvider.Phone },
        PickupAddress = o.PickupAddress.ToDto(),
        ProviderAddress = o.ServiceProvider.Address?.ToDto(),
        PickupDate = o.PickupDate,
        PickupSlot = o.PickupSlot,
        DriverNotes = o.DriverNotes,
        Items = o.Items.OrderBy(i => i.CreatedAt).Select(i => i.ToDto()).ToList(),
        SubTotal = o.SubTotal,
        Tax = o.Tax,
        VatRate = o.VatRate,
        DeliveryFee = o.DeliveryFee,
        ExtraCharges = o.ExtraCharges.OrderBy(c => c.DisplayOrder).ThenBy(c => c.CreatedAt).Select(c => new OrderExtraChargeDto { Name = c.Name, Amount = c.Amount, Basis = c.Basis }).ToList(),
        TotalAmount = o.TotalAmount,
        Currency = currency,
        IsPaid = o.IsPaid(),
        DriverHandedOver = o.DriverHandedOver(),
        Rating = o.Rating?.ToDto(o.OrderNumber, o.Customer.User.Name),
        CanRate = includeCustomerCodes && o.Status == OrderStatus.Completed && o.Rating is null,
        History = o.StatusHistory.OrderBy(h => h.CreatedAt).Select(h => new StatusHistoryDto
        {
            Status = h.Status,
            Notes = h.Notes,
            ChangedByUserId = h.ChangedByUserId,
            CreatedAt = h.CreatedAt
        }).ToList(),
        Photos = o.Photos.OrderBy(p => p.CreatedAt).Select(p => p.ToDto()).ToList(),
        DriverJobs = o.DriverJobs.OrderBy(j => j.CreatedAt).Select(j => j.ToDto(o, driverEarning)).ToList(),
        Payments = o.Payments.OrderBy(p => p.CreatedAt).Select(p => p.ToDto()).ToList(),
        Disputes = o.Disputes.OrderBy(d => d.CreatedAt).Select(d => d.ToDto()).ToList(),
        PickupCode = includeCustomerCodes ? o.Verifications.FirstOrDefault(v => v.Type == VerificationType.QR && v.VerifiedAt == null)?.OtpCode : null,
        DeliveryOtp = includeCustomerCodes ? o.Verifications.FirstOrDefault(v => v.Type == VerificationType.OTP && v.VerifiedAt == null)?.OtpCode : null,
        CreatedAt = o.CreatedAt,
        UpdatedAt = o.UpdatedAt
    };

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
