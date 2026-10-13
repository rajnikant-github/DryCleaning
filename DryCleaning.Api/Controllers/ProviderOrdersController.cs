using DryCleaning.Api.Common;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DryCleaning.Api.Controllers;

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

    /// <summary>Badge counts for every order tab, including Cancelled and Disputed.</summary>
    [HttpGet("orders/counts")]
    public Task<ProviderOrderCountsDto> GetOrderCounts(CancellationToken ct) => orders.GetOrderCountsAsync(ct);

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
