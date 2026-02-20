using FixHub.API.Extensions;
using FixHub.Application.Common.Models;
using FixHub.Application.Features.Notifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixHub.API.Controllers.v1;

[Authorize]
[ApiController]
[Route("api/v1/notifications")]
public class NotificationsController(ISender mediator) : ApiControllerBase
{
    /// <summary>List notifications for the current user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetMyNotificationsQuery(CurrentUserId, page, pageSize), ct);
        return result.ToActionResult(this);
    }

    /// <summary>Get unread count for the current user.</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> UnreadCount(CancellationToken ct = default)
    {
        var count = await mediator.Send(new GetUnreadCountQuery(CurrentUserId), ct);
        return Ok(count);
    }

    /// <summary>Mark a notification as read.</summary>
    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct = default)
    {
        var result = await mediator.Send(new MarkNotificationReadCommand(id, CurrentUserId), ct);
        return result.ToActionResult(this, successStatusCode: 204);
    }
}
