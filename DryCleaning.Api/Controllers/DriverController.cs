using DryCleaning.Api.Common;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DryCleaning.Api.Controllers;

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

    /// <summary>D02 – badge counts for the Available / My Jobs / Completed tabs.</summary>
    [HttpGet("jobs/counts")]
    public Task<DriverJobCountsDto> Counts(CancellationToken ct) => drivers.GetJobCountsAsync(ct);

    /// <summary>The job a notification refers to: the caller's own job for the order, else one still on the marketplace.</summary>
    [HttpGet("jobs/by-order/{orderId:guid}")]
    public Task<DriverJobDetailDto> ByOrder(Guid orderId, CancellationToken ct) => drivers.GetJobForOrderAsync(orderId, ct);

    /// <summary>D02 – completed job history (newest first).</summary>
    [HttpGet("jobs/history")]
    public Task<PagedResult<DriverJobDto>> History([FromQuery] PagedRequest request, CancellationToken ct) =>
        drivers.GetMyJobsAsync(new DriverJobsQuery { Page = request.Page, PageSize = request.PageSize, Status = Domain.DriverJobStatus.Completed }, ct);

    /// <summary>D13 – earnings for the last 7 days (Week) or 4 weeks (Month).</summary>
    [HttpGet("earnings")]
    public Task<DriverEarningsDto> Earnings([FromQuery] EarningsPeriod period, CancellationToken ct) => drivers.GetEarningsAsync(period, ct);

    /// <summary>D03 – job details: route, items, next step; phone numbers once the job is yours.</summary>
    [HttpGet("jobs/{jobId:guid}")]
    public Task<DriverJobDetailDto> Get(Guid jobId, CancellationToken ct) => drivers.GetJobAsync(jobId, ct);

    [HttpPost("jobs/{jobId:guid}/accept")]
    public Task<DriverJobDetailDto> Accept(Guid jobId, CancellationToken ct) => drivers.AcceptAsync(jobId, ct);

    /// <summary>Give an accepted job back to the marketplace (before collecting items).</summary>
    [HttpPost("jobs/{jobId:guid}/release")]
    public Task<DriverJobDetailDto> Release(Guid jobId, CancellationToken ct) => drivers.ReleaseAsync(jobId, ct);

    /// <summary>D05/D06 – item check plus the customer's pickup code; order → PickedUp. Upload photos via /api/orders/{id}/photos (Type=Pickup).</summary>
    [HttpPost("jobs/{jobId:guid}/pickup")]
    public Task<DriverJobDetailDto> ConfirmPickup(Guid jobId, DriverPickupRequest request, CancellationToken ct) => drivers.ConfirmPickupAsync(jobId, request, ct);

    /// <summary>D08 – hand items over to the provider (Code is ignored; Notes optional).</summary>
    [HttpPost("jobs/{jobId:guid}/handover")]
    public Task<DriverJobDetailDto> ConfirmHandover(Guid jobId, VerifyCodeRequest request, CancellationToken ct) => drivers.ConfirmHandoverAsync(jobId, request, ct);

    /// <summary>D09 – collected from provider (optional package count and notes); order → OutForDelivery.</summary>
    [HttpPost("jobs/{jobId:guid}/start-delivery")]
    public Task<DriverJobDetailDto> StartDelivery(
        Guid jobId,
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] StartDeliveryRequest? request,
        CancellationToken ct) => drivers.StartDeliveryAsync(jobId, request, ct);

    /// <summary>D11 – confirm delivery with the customer's OTP (completes the order). Upload proof via /api/orders/{id}/photos (Type=Completion).</summary>
    [HttpPost("jobs/{jobId:guid}/deliver")]
    public Task<DriverJobDetailDto> ConfirmDelivery(Guid jobId, VerifyCodeRequest request, CancellationToken ct) => drivers.ConfirmDeliveryAsync(jobId, request, ct);
}
