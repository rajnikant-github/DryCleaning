using DryCleaning.Api.Common;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DryCleaning.Api.Controllers;

/// <summary>Admin web portal (A02–A12).</summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = Roles.Admin)]
[Produces("application/json")]
public sealed class AdminController(AdminService admin, CategoryService categories) : ControllerBase
{
    /// <summary>A02 – totals, revenue, disputes and order charts.</summary>
    [HttpGet("dashboard")]
    public Task<AdminDashboardDto> Dashboard(CancellationToken ct) => admin.GetDashboardAsync(ct);

    // A03 Customers
    [HttpGet("customers")]
    public Task<PagedResult<AdminCustomerDto>> Customers([FromQuery] PagedRequest request, CancellationToken ct) => admin.GetCustomersAsync(request, ct);

    /// <summary>Deactivate / reactivate any user account.</summary>
    [HttpPut("users/{userId:guid}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetUserActive(Guid userId, SetActiveRequest request, CancellationToken ct)
    {
        await admin.SetUserActiveAsync(userId, request.IsActive, ct);
        return NoContent();
    }

    // A04 Providers
    [HttpGet("providers")]
    public Task<PagedResult<AdminProviderDto>> Providers([FromQuery] PagedRequest request, [FromQuery] bool? isActive, CancellationToken ct) =>
        admin.GetProvidersAsync(request, isActive, ct);

    /// <summary>Approve (true) or deactivate (false) a provider.</summary>
    [HttpPut("providers/{providerId:guid}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetProviderActive(Guid providerId, SetActiveRequest request, CancellationToken ct)
    {
        await admin.SetProviderActiveAsync(providerId, request.IsActive, ct);
        return NoContent();
    }

    /// <summary>A07 – provider catalog (categories → products → service items).</summary>
    [HttpGet("providers/{providerId:guid}/catalog")]
    public Task<ProviderCatalogDto> ProviderCatalog(Guid providerId, CancellationToken ct) => admin.GetProviderCatalogAsync(providerId, ct);

    // A05 Drivers (shared marketplace drivers – no ServiceProviderId)
    [HttpGet("drivers")]
    public Task<PagedResult<AdminDriverDto>> Drivers([FromQuery] PagedRequest request, CancellationToken ct) => admin.GetDriversAsync(request, ct);

    [HttpPut("drivers/{driverId:guid}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetDriverActive(Guid driverId, SetActiveRequest request, CancellationToken ct)
    {
        await admin.SetDriverActiveAsync(driverId, request.IsActive, ct);
        return NoContent();
    }

    // A06 Categories
    [HttpGet("categories")]
    public Task<IReadOnlyList<CategoryDto>> Categories(CancellationToken ct) => categories.GetAsync(includeInactive: true, ct);

    [HttpPost("categories")]
    public Task<CategoryDto> CreateCategory(CategoryRequest request, CancellationToken ct) => categories.CreateAsync(request, ct);

    [HttpPut("categories/{id:guid}")]
    public Task<CategoryDto> UpdateCategory(Guid id, CategoryRequest request, CancellationToken ct) => categories.UpdateAsync(id, request, ct);

    [HttpPut("categories/{id:guid}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetCategoryActive(Guid id, SetActiveRequest request, CancellationToken ct)
    {
        await categories.SetActiveAsync(id, request.IsActive, ct);
        return NoContent();
    }

    [HttpPut("categories/order")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReorderCategories(ReorderRequest request, CancellationToken ct)
    {
        await categories.ReorderAsync(request, ct);
        return NoContent();
    }

    // A08 / A09 Orders
    [HttpGet("orders")]
    public Task<PagedResult<AdminOrderDto>> Orders([FromQuery] AdminOrdersQuery query, CancellationToken ct) => admin.GetOrdersAsync(query, ct);

    /// <summary>Full lifecycle, items, photos, verification, payments and status history.</summary>
    [HttpGet("orders/{orderId:guid}")]
    public Task<OrderDetailDto> Order(Guid orderId, CancellationToken ct) => admin.GetOrderAsync(orderId, ct);

    // A10 Driver jobs
    [HttpGet("driver-jobs")]
    public Task<PagedResult<DriverJobDto>> DriverJobs([FromQuery] AdminJobsQuery query, CancellationToken ct) => admin.GetDriverJobsAsync(query, ct);

    /// <summary>Manually assign or reassign a driver.</summary>
    [HttpPost("driver-jobs/{jobId:guid}/assign")]
    public Task<DriverJobDto> AssignDriver(Guid jobId, AssignDriverRequest request, CancellationToken ct) => admin.AssignDriverAsync(jobId, request, ct);

    // A11 Payments
    [HttpGet("payments")]
    public Task<PagedResult<AdminPaymentDto>> Payments([FromQuery] PagedRequest request, [FromQuery] PaymentStatus? status, CancellationToken ct) =>
        admin.GetPaymentsAsync(request, status, ct);

    // A12 Disputes
    [HttpGet("disputes")]
    public Task<PagedResult<DisputeDto>> Disputes([FromQuery] AdminDisputesQuery query, CancellationToken ct) => admin.GetDisputesAsync(query, ct);

    [HttpPost("disputes/{disputeId:guid}/review")]
    public Task<DisputeDto> ReviewDispute(Guid disputeId, CancellationToken ct) => admin.ReviewDisputeAsync(disputeId, ct);

    /// <summary>Resolve or reject; optionally move the order to a new status (e.g. Processing, Cancelled, Completed).</summary>
    [HttpPost("disputes/{disputeId:guid}/resolve")]
    public Task<DisputeDto> ResolveDispute(Guid disputeId, ResolveDisputeRequest request, CancellationToken ct) =>
        admin.ResolveDisputeAsync(disputeId, request, ct);
}
