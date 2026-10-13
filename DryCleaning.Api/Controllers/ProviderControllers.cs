using DryCleaning.Api.Common;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DryCleaning.Api.Controllers;

/// <summary>Service provider business profile and enabled categories.</summary>
[ApiController]
[Route("api/provider")]
[Authorize(Roles = Roles.Provider)]
[Produces("application/json")]
public sealed class ProviderProfileController(ProviderProfileService profiles, ProviderCatalogService catalog) : ControllerBase
{
    [HttpGet("profile")]
    public Task<ProviderProfileDto> GetProfile(CancellationToken ct) => profiles.GetAsync(ct);

    [HttpPut("profile")]
    public Task<ProviderProfileDto> UpdateProfile(UpdateProviderProfileRequest request, CancellationToken ct) => profiles.UpdateAsync(request, ct);

    /// <summary>All global categories with whether my business offers them.</summary>
    [HttpGet("categories")]
    public Task<IReadOnlyList<ProviderCategoryDto>> GetCategories(CancellationToken ct) => catalog.GetCategoriesAsync(ct);

    /// <summary>Choose which global categories my business offers (providers can't create categories).</summary>
    [HttpPut("categories")]
    public Task<IReadOnlyList<ProviderCategoryDto>> SetCategories(SetProviderCategoriesRequest request, CancellationToken ct) =>
        catalog.SetCategoriesAsync(request, ct);
}

/// <summary>P11 – Products and Service Items (Category → Product → Service Item).</summary>
[ApiController]
[Route("api/provider")]
[Authorize(Roles = Roles.Provider)]
[Produces("application/json")]
public sealed class ProviderCatalogController(ProviderCatalogService catalog) : ControllerBase
{
    [HttpGet("products")]
    public Task<IReadOnlyList<ProductDto>> GetProducts([FromQuery] Guid? categoryId, CancellationToken ct) => catalog.GetProductsAsync(categoryId, ct);

    [HttpGet("products/{productId:guid}")]
    public Task<ProductDto> GetProduct(Guid productId, CancellationToken ct) => catalog.GetProductAsync(productId, ct);

    [HttpPost("products")]
    public Task<ProductDto> CreateProduct(ProductRequest request, CancellationToken ct) => catalog.CreateProductAsync(request, ct);

    [HttpPut("products/{productId:guid}")]
    public Task<ProductDto> UpdateProduct(Guid productId, ProductRequest request, CancellationToken ct) => catalog.UpdateProductAsync(productId, request, ct);

    /// <summary>Enable / disable a product.</summary>
    [HttpPut("products/{productId:guid}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetProductActive(Guid productId, SetActiveRequest request, CancellationToken ct)
    {
        await catalog.SetProductActiveAsync(productId, request.IsActive, ct);
        return NoContent();
    }

    [HttpPost("products/{productId:guid}/service-items")]
    public Task<ServiceItemDto> CreateServiceItem(Guid productId, ServiceItemRequest request, CancellationToken ct) =>
        catalog.CreateServiceItemAsync(productId, request, ct);

    /// <summary>Edit name, price and estimated days.</summary>
    [HttpPut("service-items/{serviceItemId:guid}")]
    public Task<ServiceItemDto> UpdateServiceItem(Guid serviceItemId, ServiceItemRequest request, CancellationToken ct) =>
        catalog.UpdateServiceItemAsync(serviceItemId, request, ct);

    [HttpPut("service-items/{serviceItemId:guid}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetServiceItemActive(Guid serviceItemId, SetActiveRequest request, CancellationToken ct)
    {
        await catalog.SetServiceItemActiveAsync(serviceItemId, request.IsActive, ct);
        return NoContent();
    }
}

/// <summary>Provider order workflow: accept → receive → inspect → process → quality check → ready → request driver.</summary>
[ApiController]
[Route("api/provider")]
[Authorize(Roles = Roles.Provider)]
[Produces("application/json")]
public sealed class ProviderOrdersController(ProviderOrderService orders) : ControllerBase
{
    /// <summary>P02 – counts, today's orders and recent activity.</summary>
    [HttpGet("dashboard")]
    public Task<ProviderDashboardDto> Dashboard(CancellationToken ct) => orders.GetDashboardAsync(ct);

    /// <summary>Orders by filter: New, Incoming, InProcess, Ready, Completed, Cancelled, Disputed, All.</summary>
    [HttpGet("orders")]
    public Task<PagedResult<OrderSummaryDto>> GetOrders([FromQuery] ProviderOrdersQuery query, CancellationToken ct) => orders.GetOrdersAsync(query, ct);

    /// <summary>P04 – order details.</summary>
    [HttpGet("orders/{id:guid}")]
    public Task<OrderDetailDto> GetOrder(Guid id, CancellationToken ct) => orders.GetOrderAsync(id, ct);

    /// <summary>Accept a new (paid) order – publishes a pickup job to marketplace drivers.</summary>
    [HttpPost("orders/{id:guid}/accept")]
    public Task<OrderDetailDto> Accept(Guid id, CancellationToken ct) => orders.AcceptAsync(id, ct);

    [HttpPost("orders/{id:guid}/reject")]
    public Task<OrderDetailDto> Reject(Guid id, ReasonRequest request, CancellationToken ct) => orders.RejectAsync(id, request, ct);

    /// <summary>P05 – confirm items received from the driver.</summary>
    [HttpPost("orders/{id:guid}/receive")]
    public Task<OrderDetailDto> Receive(Guid id, ReceiveOrderRequest request, CancellationToken ct) => orders.ConfirmReceivedAsync(id, request, ct);

    /// <summary>P06 – complete inspection (moves to Processing). Upload photos via /api/orders/{id}/photos.</summary>
    [HttpPost("orders/{id:guid}/inspection")]
    public Task<OrderDetailDto> CompleteInspection(Guid id, InspectionRequest request, CancellationToken ct) =>
        orders.CompleteInspectionAsync(id, request, ct);

    /// <summary>P07 – cleaning finished, send to quality check.</summary>
    [HttpPost("orders/{id:guid}/quality-check")]
    public Task<OrderDetailDto> MarkQualityCheck(Guid id, CancellationToken ct) => orders.MarkQualityCheckAsync(id, ct);

    /// <summary>P08 – quality check failed, back to processing.</summary>
    [HttpPost("orders/{id:guid}/reprocess")]
    public Task<OrderDetailDto> SendBack(Guid id, ReasonRequest request, CancellationToken ct) => orders.SendBackToProcessingAsync(id, request, ct);

    /// <summary>P08 – mark ready for delivery.</summary>
    [HttpPost("orders/{id:guid}/ready")]
    public Task<OrderDetailDto> MarkReady(Guid id, CancellationToken ct) => orders.MarkReadyForDeliveryAsync(id, ct);

    /// <summary>P09 – publish a delivery job to marketplace drivers.</summary>
    [HttpPost("orders/{id:guid}/request-driver")]
    public Task<OrderDetailDto> RequestDriver(Guid id, CancellationToken ct) => orders.RequestDeliveryDriverAsync(id, ct);

    /// <summary>Report a discrepancy or damage without stopping the order.</summary>
    [HttpPost("orders/{id:guid}/issues")]
    public Task<DisputeDto> ReportIssue(Guid id, IssueRequest request, CancellationToken ct) => orders.ReportIssueAsync(id, request, ct);

    /// <summary>Raise a dispute and put the order on hold.</summary>
    [HttpPost("orders/{id:guid}/disputes")]
    public Task<DisputeDto> RaiseDispute(Guid id, IssueRequest request, CancellationToken ct) => orders.RaiseDisputeAsync(id, request, ct);
}

/// <summary>Shared marketplace driver: availability, jobs, pickup, handover and delivery.</summary>
[ApiController]
[Route("api/driver")]
[Authorize(Roles = Roles.Driver)]
[Produces("application/json")]
public sealed class DriverController(DriverService drivers) : ControllerBase
{
    [HttpGet("profile")]
    public Task<DriverProfileDto> GetProfile(CancellationToken ct) => drivers.GetProfileAsync(ct);

    [HttpPut("profile")]
    public Task<DriverProfileDto> UpdateProfile(UpdateDriverProfileRequest request, CancellationToken ct) => drivers.UpdateProfileAsync(request, ct);

    /// <summary>D02 – availability, available jobs, active job, completed today.</summary>
    [HttpGet("dashboard")]
    public Task<DriverDashboardDto> Dashboard(CancellationToken ct) => drivers.GetDashboardAsync(ct);

    /// <summary>Go online / offline.</summary>
    [HttpPut("availability")]
    public Task<bool> SetAvailability(SetActiveRequest request, CancellationToken ct) => drivers.SetAvailabilityAsync(request.IsActive, ct);

    /// <summary>D03 – jobs waiting for a driver (pickup and delivery).</summary>
    [HttpGet("jobs/available")]
    public Task<PagedResult<DriverJobDto>> Available([FromQuery] PagedRequest request, CancellationToken ct) => drivers.GetAvailableJobsAsync(request, ct);

    /// <summary>D02 – my jobs (filter by status: Assigned/InProgress for active, Completed/Cancelled for history).</summary>
    [HttpGet("jobs")]
    public Task<PagedResult<DriverJobDto>> Mine([FromQuery] DriverJobsQuery query, CancellationToken ct) => drivers.GetMyJobsAsync(query, ct);

    /// <summary>D02 – completed job history (newest first).</summary>
    [HttpGet("jobs/history")]
    public Task<PagedResult<DriverJobDto>> History([FromQuery] PagedRequest request, CancellationToken ct) =>
        drivers.GetMyJobsAsync(new DriverJobsQuery { Page = request.Page, PageSize = request.PageSize, Status = Domain.DriverJobStatus.Completed }, ct);

    /// <summary>D13 – earnings for the last 7 days (Week) or 4 weeks (Month).</summary>
    [HttpGet("earnings")]
    public Task<DriverEarningsDto> Earnings([FromQuery] EarningsPeriod period, CancellationToken ct) => drivers.GetEarningsAsync(period, ct);

    /// <summary>D04 – job details.</summary>
    [HttpGet("jobs/{jobId:guid}")]
    public Task<DriverJobDto> Get(Guid jobId, CancellationToken ct) => drivers.GetJobAsync(jobId, ct);

    [HttpPost("jobs/{jobId:guid}/accept")]
    public Task<DriverJobDto> Accept(Guid jobId, CancellationToken ct) => drivers.AcceptAsync(jobId, ct);

    /// <summary>Give an accepted job back to the marketplace (before collecting items).</summary>
    [HttpPost("jobs/{jobId:guid}/release")]
    public Task<DriverJobDto> Release(Guid jobId, CancellationToken ct) => drivers.ReleaseAsync(jobId, ct);

    /// <summary>D06 – verify the customer's pickup code (QR) and collect items.</summary>
    [HttpPost("jobs/{jobId:guid}/pickup")]
    public Task<DriverJobDto> ConfirmPickup(Guid jobId, VerifyCodeRequest request, CancellationToken ct) => drivers.ConfirmPickupAsync(jobId, request, ct);

    /// <summary>D08 – hand items over to the provider.</summary>
    [HttpPost("jobs/{jobId:guid}/handover")]
    public Task<DriverJobDto> ConfirmHandover(Guid jobId, VerifyCodeRequest request, CancellationToken ct) => drivers.ConfirmHandoverAsync(jobId, request, ct);

    /// <summary>D10 – collected from provider, out for delivery.</summary>
    [HttpPost("jobs/{jobId:guid}/start-delivery")]
    public Task<DriverJobDto> StartDelivery(Guid jobId, CancellationToken ct) => drivers.StartDeliveryAsync(jobId, ct);

    /// <summary>D11 – confirm delivery with the customer's OTP (completes the order).</summary>
    [HttpPost("jobs/{jobId:guid}/deliver")]
    public Task<DriverJobDto> ConfirmDelivery(Guid jobId, VerifyCodeRequest request, CancellationToken ct) => drivers.ConfirmDeliveryAsync(jobId, request, ct);
}
