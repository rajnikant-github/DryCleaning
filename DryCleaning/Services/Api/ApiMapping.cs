using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services.Api.Contracts;

namespace DryCleaning.Services.Api;

/// <summary>Converts API contracts into the app's read models (the same models the mock services return).</summary>
public static class ApiMapping
{
    // ── Dates ────────────────────────────────────────────────────────────────

    /// <summary>The API stores UTC; values without an offset are UTC too. Shown in the phone's time zone.</summary>
    public static DateTime Local(DateTime value) => value.Kind switch
    {
        DateTimeKind.Local => value,
        DateTimeKind.Utc => value.ToLocalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime()
    };

    public static DateTime? Local(DateTime? value) => value is { } v ? Local(v) : null;

    /// <summary>Pickup dates are calendar dates – no time-zone shift.</summary>
    public static DateTime? CalendarDate(DateTime? value) => value?.Date;

    // ── Users & addresses ────────────────────────────────────────────────────

    public static User ToUser(UserDto dto) => new()
    {
        Id = dto.Id,
        Role = dto.Role,
        Name = dto.Name,
        Email = dto.Email ?? string.Empty,
        Phone = dto.Phone ?? string.Empty,
        IsActive = dto.IsActive
    };

    public static Address ToAddress(AddressDto dto) => new()
    {
        Id = dto.Id,
        GooglePlaceId = dto.GooglePlaceId,
        FormattedAddress = dto.FormattedAddress,
        AddressLine1 = dto.AddressLine1,
        AddressLine2 = dto.AddressLine2,
        StreetNumber = dto.StreetNumber,
        Route = dto.Route,
        SubLocality = dto.SubLocality,
        Locality = dto.Locality,
        AdministrativeAreaLevel1 = dto.AdministrativeAreaLevel1,
        AdministrativeAreaLevel2 = dto.AdministrativeAreaLevel2,
        Country = dto.Country,
        CountryCode = dto.CountryCode,
        PostalCode = dto.PostalCode,
        Latitude = dto.Latitude,
        Longitude = dto.Longitude,
        PlusCode = dto.PlusCode
    };

    public static UpsertAddressRequest ToUpsert(Address a) => new()
    {
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
        Latitude = Math.Round(a.Latitude, 7),
        Longitude = Math.Round(a.Longitude, 7),
        PlusCode = a.PlusCode
    };

    // ── Catalog ──────────────────────────────────────────────────────────────

    public static Category ToCategory(CategoryDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Description = dto.Description ?? string.Empty,
        ImageUrl = dto.ImageUrl,
        DisplayOrder = dto.DisplayOrder,
        IsActive = dto.IsActive
    };

    public static ProviderListing ToListing(ProviderSummaryDto dto) => new()
    {
        Id = dto.Id,
        BusinessName = dto.BusinessName,
        Description = dto.Description,
        Locality = dto.Locality,
        Categories = dto.Categories,
        StartingPrice = dto.StartingPrice,
        FastestDays = dto.FastestDays
    };

    public static ServiceItem ToServiceItem(ServiceItemDto dto) => new()
    {
        Id = dto.Id,
        ProductId = dto.ProductId,
        Name = dto.Name,
        Description = dto.Description ?? string.Empty,
        ImageUrl = dto.ImageUrl,
        Price = dto.Price,
        EstimatedDays = dto.EstimatedDays,
        DisplayOrder = dto.DisplayOrder,
        IsActive = dto.IsActive
    };

    public static Product ToProduct(ProductDto dto) => new()
    {
        Id = dto.Id,
        ServiceProviderId = dto.ServiceProviderId,
        CategoryId = dto.CategoryId,
        Name = dto.Name,
        Description = dto.Description ?? string.Empty,
        ImageUrl = dto.ImageUrl,
        DisplayOrder = dto.DisplayOrder,
        IsActive = dto.IsActive
    };

    public static CatalogProduct ToCatalogProduct(ProductDto dto)
    {
        var items = dto.ServiceItems.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).Select(ToServiceItem).ToList();
        return new CatalogProduct
        {
            Id = dto.Id,
            ProviderId = dto.ServiceProviderId,
            CategoryId = dto.CategoryId,
            CategoryName = dto.CategoryName,
            Name = dto.Name,
            Description = dto.Description,
            StartingPrice = items.Count == 0 ? null : items.Min(s => s.Price),
            ServiceItems = items
        };
    }

    public static PopularServiceItem ToPopular(PopularServiceItemDto dto) => new()
    {
        ServiceItemId = dto.ServiceItemId,
        ProductId = dto.ProductId,
        ProviderId = dto.ProviderId,
        ProviderName = dto.ProviderName,
        ProductName = dto.ProductName,
        ServiceItemName = dto.ServiceItemName,
        Price = dto.Price,
        EstimatedDays = dto.EstimatedDays
    };

    // ── Orders ───────────────────────────────────────────────────────────────

    public static OrderSummary ToSummary(OrderSummaryDto dto) => new()
    {
        Id = dto.Id,
        OrderNumber = dto.OrderNumber,
        CustomerName = dto.CustomerName,
        ProviderName = dto.ProviderName,
        Status = dto.Status,
        ItemCount = dto.ItemCount,
        ItemsText = dto.ItemsText,
        TotalAmount = dto.TotalAmount,
        IsPaid = dto.IsPaid,
        HasOpenDispute = dto.HasOpenDispute,
        PickupDate = CalendarDate(dto.PickupDate),
        PickupSlot = dto.PickupSlot,
        CreatedAt = Local(dto.CreatedAt),
        UpdatedAt = Local(dto.UpdatedAt)
    };

    public static OrderItemDetail ToItemDetail(OrderItemDto dto) => new()
    {
        Id = dto.Id,
        ProductId = dto.ProductId,
        ServiceItemId = dto.ServiceItemId,
        CategoryName = dto.CategoryName,
        ProductName = dto.ProductName,
        ServiceItemName = dto.ServiceItemName,
        Quantity = dto.Quantity,
        UnitPrice = dto.UnitPrice,
        TotalPrice = dto.TotalPrice,
        Notes = dto.Notes,
        EstimatedDays = dto.EstimatedDays,
        Icon = CatalogIcons.ForProduct(dto.ProductName)
    };

    public static OrderDetail ToDetail(OrderDetailDto dto, Func<string, string> absoluteUrl)
    {
        var jobs = dto.DriverJobs.Where(j => j.Status != DriverJobStatus.Cancelled).OrderBy(j => j.CreatedAt).ToList();
        var payment = dto.Payments.OrderBy(p => p.CreatedAt).LastOrDefault(p => p.Status == PaymentStatus.Success)
                      ?? dto.Payments.OrderBy(p => p.CreatedAt).LastOrDefault();
        var dispute = dto.Disputes.OrderBy(d => d.CreatedAt).LastOrDefault(d => d.Status is DisputeStatus.Open or DisputeStatus.UnderReview);

        return new OrderDetail
        {
            Order = new Order
            {
                Id = dto.Id,
                OrderNumber = dto.OrderNumber,
                CustomerId = dto.Customer.Id,
                ServiceProviderId = dto.Provider.Id,
                PickupAddressId = dto.PickupAddress.Id,
                Status = dto.Status,
                SubTotal = dto.SubTotal,
                Tax = dto.Tax,
                DeliveryFee = dto.DeliveryFee,
                TotalAmount = dto.TotalAmount,
                PickupDate = CalendarDate(dto.PickupDate),
                PickupSlot = dto.PickupSlot,
                CreatedAt = Local(dto.CreatedAt),
                UpdatedAt = Local(dto.UpdatedAt)
            },
            CustomerName = dto.Customer.Name,
            CustomerPhone = dto.Customer.Phone ?? string.Empty,
            PickupAddress = ToAddress(dto.PickupAddress),
            Items = dto.Items.Select(ToItemDetail).ToList(),
            History = dto.History.OrderBy(h => h.CreatedAt).Select(h => new OrderStatusHistory
            {
                OrderId = dto.Id,
                Status = h.Status,
                ChangedByUserId = h.ChangedByUserId,
                Notes = h.Notes,
                CreatedAt = Local(h.CreatedAt),
                UpdatedAt = Local(h.CreatedAt)
            }).ToList(),
            Photos = dto.Photos.OrderBy(p => p.CreatedAt).Select(p => new OrderPhoto
            {
                Id = p.Id,
                OrderId = dto.Id,
                Type = p.Type,
                Url = absoluteUrl(p.Url),
                UploadedBy = p.UploadedBy,
                CreatedAt = Local(p.CreatedAt),
                UpdatedAt = Local(p.CreatedAt)
            }).ToList(),
            PickupDriver = ToAssignment(jobs.LastOrDefault(j => j.JobType == DriverJobType.Pickup && j.DriverId != null)),
            DeliveryDriver = ToAssignment(jobs.LastOrDefault(j => j.JobType == DriverJobType.Delivery && j.DriverId != null)),
            AwaitingDriverFor = jobs.LastOrDefault(j => j.Status == DriverJobStatus.Pending)?.JobType,
            Payment = payment is null ? null : ToPayment(payment, dto.Id),
            OpenDispute = dispute is null ? null : new Dispute
            {
                Id = dispute.Id,
                OrderId = dispute.OrderId,
                RaisedByUserId = dispute.RaisedByUserId,
                Reason = dispute.Reason,
                Description = dispute.Description ?? string.Empty,
                Status = dispute.Status,
                Resolution = dispute.Resolution,
                CreatedAt = Local(dispute.CreatedAt)
            },
            ProviderName = dto.Provider.Name,
            ProviderPhone = dto.Provider.Phone ?? string.Empty,
            ProviderAddress = dto.ProviderAddress is null ? null : ToAddress(dto.ProviderAddress),
            PickupCode = dto.PickupCode,
            DeliveryOtp = dto.DeliveryOtp
        };
    }

    public static Payment ToPayment(PaymentDto dto, Guid orderId) => new()
    {
        Id = dto.Id,
        OrderId = orderId,
        Amount = dto.Amount,
        Status = dto.Status,
        Method = dto.Method,
        TransactionId = dto.TransactionId,
        PaidAt = Local(dto.PaidAt),
        CreatedAt = Local(dto.CreatedAt)
    };

    private static DriverAssignment? ToAssignment(DriverJobDto? job) =>
        job?.DriverId is null
            ? null
            : new DriverAssignment
            {
                DriverJobId = job.Id,
                JobType = job.JobType,
                Status = job.Status,
                DriverName = job.DriverName ?? "Driver",
                DriverPhone = job.DriverPhone ?? string.Empty,
                LicenseNumber = string.Empty,
                AssignedAt = Local(job.AssignedAt),
                CompletedAt = Local(job.CompletedAt)
            };

    public static ActivityEntry ToActivity(ActivityDto dto) => new()
    {
        OrderId = dto.OrderId,
        OrderNumber = dto.OrderNumber,
        Status = dto.Status,
        Text = $"{dto.OrderNumber} · {dto.Status.TimelineTitle()}",
        At = Local(dto.At)
    };

    public static Notification ToNotification(NotificationDto dto) => new()
    {
        Id = dto.Id,
        OrderId = dto.OrderId,
        Title = dto.Title,
        Message = dto.Message,
        Type = dto.Type,
        IsRead = dto.IsRead,
        CreatedAt = Local(dto.CreatedAt),
        UpdatedAt = Local(dto.CreatedAt)
    };

    // ── Driver ───────────────────────────────────────────────────────────────

    public static DriverJobSummary ToJobSummary(DriverJobDto dto)
    {
        var from = ToAddress(dto.PickupAddress);
        var to = ToAddress(dto.DeliveryAddress);
        return new DriverJobSummary
        {
            Id = dto.Id,
            OrderId = dto.OrderId,
            OrderNumber = dto.OrderNumber,
            JobType = dto.JobType,
            Status = dto.Status,
            OrderStatus = dto.OrderStatus,
            FromName = dto.PickupName,
            FromAddress = from.FormattedAddress,
            FromArea = from.SubLocality ?? from.Locality ?? string.Empty,
            ToName = dto.DeliveryName,
            ToAddress = to.FormattedAddress,
            ToArea = to.SubLocality ?? to.Locality ?? string.Empty,
            ItemCount = dto.ItemCount,
            ItemsText = dto.ItemCount == 1 ? "1 item" : $"{dto.ItemCount} items",
            Earnings = dto.EstimatedEarnings,
            DistanceKm = Geo.DistanceKm(from, to),
            PickupDate = CalendarDate(dto.PickupDate),
            PickupSlot = dto.PickupSlot,
            CreatedAt = Local(dto.CreatedAt),
            AssignedAt = Local(dto.AssignedAt),
            CompletedAt = Local(dto.CompletedAt),
            IsOnHold = dto.IsOnHold
        };
    }

    public static DriverJobDetail ToJobDetail(DriverJobDetailDto dto)
    {
        var summary = ToJobSummary(dto.Job);
        var items = dto.Items.Select(ToItemDetail).ToList();
        var isPickup = dto.Job.JobType == DriverJobType.Pickup;
        if (items.Count > 0)
        {
            // The job list only has a count; the detail can say what's in the bag.
            summary = CopyWithItemsText(summary, string.Join(", ", items.Select(i => $"{i.Quantity} {i.ProductName.ToLowerInvariant()}")));
        }

        return new DriverJobDetail
        {
            Job = summary,
            From = ToAddress(dto.Job.PickupAddress),
            To = ToAddress(dto.Job.DeliveryAddress),
            FromPhone = (isPickup ? dto.CustomerPhone : dto.ProviderPhone) ?? string.Empty,
            ToPhone = (isPickup ? dto.ProviderPhone : dto.CustomerPhone) ?? string.Empty,
            CustomerName = dto.CustomerName,
            ProviderName = dto.ProviderName,
            Items = items,
            NextStep = dto.NextStep,
            IsMine = dto.IsMine
        };
    }

    private static DriverJobSummary CopyWithItemsText(DriverJobSummary s, string itemsText) => new()
    {
        Id = s.Id,
        OrderId = s.OrderId,
        OrderNumber = s.OrderNumber,
        JobType = s.JobType,
        Status = s.Status,
        OrderStatus = s.OrderStatus,
        FromName = s.FromName,
        FromAddress = s.FromAddress,
        FromArea = s.FromArea,
        ToName = s.ToName,
        ToAddress = s.ToAddress,
        ToArea = s.ToArea,
        ItemCount = s.ItemCount,
        ItemsText = itemsText,
        Earnings = s.Earnings,
        DistanceKm = s.DistanceKm,
        PickupDate = s.PickupDate,
        PickupSlot = s.PickupSlot,
        CreatedAt = s.CreatedAt,
        AssignedAt = s.AssignedAt,
        CompletedAt = s.CompletedAt,
        IsOnHold = s.IsOnHold
    };
}
