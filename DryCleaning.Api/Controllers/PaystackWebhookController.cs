using DryCleaning.Api.Common;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DryCleaning.Api.Controllers;

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
