using DryCleaning.Api.Common;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DryCleaning.Api.Controllers;

/// <summary>Phones registered for push notifications (Firebase Cloud Messaging).</summary>
[ApiController]
[Route("api/me/devices")]
[Authorize]
[Produces("application/json")]
public sealed class DevicesController(DeviceService devices) : ControllerBase
{
    /// <summary>Register this phone's FCM token. Call after sign-in, on app start and when Firebase issues a new token.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Register(RegisterDeviceRequest request, CancellationToken ct)
    {
        await devices.RegisterAsync(request, ct);
        return NoContent();
    }

    /// <summary>Stop pushes to this phone (call on logout).</summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unregister([FromQuery] string token, CancellationToken ct)
    {
        await devices.UnregisterAsync(token, ct);
        return NoContent();
    }
}
