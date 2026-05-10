using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixPro.Services.Notifications.Application.Commands.CreateNotification;
using PixPro.Services.Notifications.Application.Commands.MarkAsRead;
using PixPro.Services.Notifications.Application.Commands.MarkAllAsRead;
using PixPro.Services.Notifications.Application.Commands.DeleteNotification;
using PixPro.Services.Notifications.Application.Queries.GetUserNotifications;
using PixPro.Services.Notifications.Application.Queries.GetUnreadCount;
using PixPro.Services.Notifications.Application.Queries.GetNotificationById;
using PixPro.Services.Notifications.Application.Queries.GetUnreadNotifications;
using PixPro.Services.Notifications.Application.DTOs.Requests;
using PixPro.Services.Notifications.Application.Common.Results;

namespace PixPro.Services.Notifications.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        IMediator mediator,
        ILogger<NotificationsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Create notification using CQRS pattern with MediatR
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(
            nameof(GetNotification),
            new { id = result.Value!.Id },
            result.Value
        );
    }
    /// <summary>
    /// Get notification by ID using CQRS pattern with MediatR
    /// </summary>
    [Authorize]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetNotification(string id)
    {
        var query = new GetNotificationByIdQuery(id);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Value);
    }
    /// <summary>
    /// Get user notifications using CQRS pattern with MediatR
    /// </summary>
    [Authorize]
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserNotifications(
        string userId,
        [FromQuery] int pageSize = 20,
        [FromQuery] int page = 1)
    {
        var query = new GetUserNotificationsQuery(userId, pageSize, page);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get unread notifications using CQRS pattern with MediatR
    /// </summary>
    [Authorize]
    [HttpGet("user/{userId}/unread")]
    public async Task<IActionResult> GetUnreadNotifications(string userId)
    {
        var query = new GetUnreadNotificationsQuery(userId);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get unread count using CQRS pattern with MediatR
    /// </summary>
    [Authorize]
    [HttpGet("user/{userId}/unread-count")]
    public async Task<IActionResult> GetUnreadCount(string userId)
    {
        var query = new GetUnreadCountQuery(userId);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { count = result.Value });
    }

    /// <summary>
    /// Mark notification as read using CQRS pattern with MediatR
    /// </summary>
    [Authorize]
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(string id)
    {
        var command = new MarkAsReadCommand(id);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Mark all notifications as read using CQRS pattern with MediatR
    /// </summary>
    [Authorize]
    [HttpPut("user/{userId}/read-all")]
    public async Task<IActionResult> MarkAllAsRead(string userId)
    {
        var command = new MarkAllAsReadCommand(userId);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Delete notification using CQRS pattern with MediatR
    /// </summary>
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(string id)
    {
        var command = new DeleteNotificationCommand(id);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }
}
