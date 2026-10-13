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
public sealed class CustomerOrdersController(CustomerOrderService orders, PaymentService payments) : ControllerBase
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

/// <summary>Paystack webhook (configure in the Paystack dashboard: https://your-host/api/payments/paystack/webhook).</summary>
[ApiController]
[Route("api/payments/paystack")]
[AllowAnonymous]
public sealed class PaystackWebhookController(PaymentService payments) : ControllerBase
{
    [HttpPost("webhook")]
    [ApiExplorerSettings(IgnoreApi = false)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);
        var ok = await payments.HandleWebhookAsync(body, Request.Headers["x-paystack-signature"].FirstOrDefault(), ct);
        return ok ? Ok() : Unauthorized();
    }
}

/// <summary>Photo verification for an order (pickup, inspection, completion, damage).</summary>
[ApiController]
[Route("api/orders/{orderId:guid}/photos")]
[Authorize]
[Produces("application/json")]
public sealed class OrderPhotosController(OrderPhotoService photos) : ControllerBase
{
    public sealed class UploadPhotoForm
    {
        /// <summary>JPG, PNG, HEIC or WEBP (max 5 MB by default).</summary>
        public IFormFile File { get; set; } = null!;

        public Domain.PhotoType Type { get; set; }
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public Task<OrderPhotoDto> Upload(Guid orderId, [FromForm] UploadPhotoForm form, CancellationToken ct) =>
        photos.UploadAsync(orderId, form.Type, form.File, ct);

    [HttpGet]
    public Task<IReadOnlyList<OrderPhotoDto>> Get(Guid orderId, CancellationToken ct) => photos.GetAsync(orderId, ct);
}
