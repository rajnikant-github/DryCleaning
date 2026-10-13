using DryCleaning.Api.Common;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DryCleaning.Api.Controllers;

/// <summary>In-app notifications for the signed-in user.</summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
[Produces("application/json")]
public sealed class NotificationsController(NotificationService notifications) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<NotificationDto>> Get([FromQuery] PagedRequest request, [FromQuery] bool unreadOnly, CancellationToken ct) =>
        notifications.GetAsync(request, unreadOnly, ct);

    [HttpGet("unread-count")]
    public Task<int> UnreadCount(CancellationToken ct) => notifications.UnreadCountAsync(ct);

    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        await notifications.MarkReadAsync(id, ct);
        return NoContent();
    }

    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await notifications.MarkAllReadAsync(ct);
        return NoContent();
    }
}
