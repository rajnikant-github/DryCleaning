using DryCleaning.Api.Common;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DryCleaning.Api.Controllers;

/// <summary>Customer orders: quote, create, pay with Paystack, track, cancel, dispute.</summary>
[ApiController]
[Route("api/customer/orders")]
[Authorize(Roles = Roles.Customer)]
[Produces("application/json")]
public sealed class CustomerOrdersController(CustomerOrderService orders, PaymentService payments, RatingService ratings) : ControllerBase
{
    /// <summary>Price a cart (subtotal, VAT, delivery fee) without creating an order.</summary>
    [HttpPost("quote")]
    public Task<OrderQuoteDto> Quote(CreateOrderRequest request, CancellationToken ct) => orders.QuoteAsync(request, ct);

    /// <summary>Create an order. Prices are calculated server-side; pickup address = my current address.</summary>
    [HttpPost]
    [ProducesResponseType<OrderDetailDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var order = await orders.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
    }

    /// <summary>My orders (Active / Completed / Cancelled).</summary>
    [HttpGet]
    public Task<PagedResult<OrderSummaryDto>> GetMine([FromQuery] CustomerOrdersQuery query, CancellationToken ct) => orders.GetMineAsync(query, ct);

    /// <summary>Badge counts for the Active / Completed / Cancelled tabs.</summary>
    [HttpGet("counts")]
    public Task<CustomerOrderCountsDto> Counts(CancellationToken ct) => orders.GetCountsAsync(ct);

    /// <summary>Order details incl. timeline, driver info, pickup code and delivery OTP.</summary>
    [HttpGet("{id:guid}")]
    public Task<OrderDetailDto> Get(Guid id, CancellationToken ct) => orders.GetAsync(id, ct);

    /// <summary>Add or change the note for the driver ("leave the parcel at the gate"). Allowed until the items are collected.</summary>
    [HttpPut("{id:guid}/driver-notes")]
    public Task<OrderDetailDto> UpdateDriverNotes(Guid id, DriverNotesRequest request, CancellationToken ct) => orders.UpdateDriverNotesAsync(id, request, ct);

    /// <summary>Rate the service provider (1 to 5 stars and an optional comment) after the order is completed. One rating per order; it can't be changed.</summary>
    [HttpPost("{id:guid}/rating")]
    public Task<RatingDto> Rate(Guid id, SubmitRatingRequest request, CancellationToken ct) => ratings.SubmitAsync(id, request, ct);

    /// <summary>Cancel before the items are collected.</summary>
    [HttpPost("{id:guid}/cancel")]
    public Task<OrderDetailDto> Cancel(Guid id, ReasonRequest request, CancellationToken ct) => orders.CancelAsync(id, request, ct);

    /// <summary>Raise a dispute (puts the order on hold).</summary>
    [HttpPost("{id:guid}/disputes")]
    public Task<DisputeDto> RaiseDispute(Guid id, IssueRequest request, CancellationToken ct) => orders.RaiseDisputeAsync(id, request, ct);

    /// <summary>Start Paystack checkout. Open the returned authorizationUrl in a browser/WebView.</summary>
    [HttpPost("{id:guid}/payments/paystack")]
    public Task<InitializePaymentResponse> InitializePaystack(Guid id, CancellationToken ct) => payments.InitializePaystackAsync(id, ct);

    /// <summary>Verify a Paystack payment after checkout returns.</summary>
    [HttpGet("{id:guid}/payments/verify")]
    public Task<PaymentDto> VerifyPayment(Guid id, [FromQuery] string reference, CancellationToken ct) =>
        payments.VerifyForCustomerAsync(id, reference, ct);

    [HttpGet("{id:guid}/payments")]
    public Task<IReadOnlyList<PaymentDto>> GetPayments(Guid id, CancellationToken ct) => payments.GetForOrderAsync(id, ct);
}
