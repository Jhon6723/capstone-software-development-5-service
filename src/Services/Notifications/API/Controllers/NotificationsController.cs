using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixPro.Services.Notifications.Application.Commands.CreateNotification;
using PixPro.Services.Notifications.Application.Commands.MarkAsRead;
using PixPro.Services.Notifications.Application.Commands.MarkAllAsRead;
using PixPro.Services.Notifications.Application.DTOs.Requests;
using PixPro.Services.Notifications.Application.Services;

namespace PixPro.Services.Notifications.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IMediator _mediator;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        IMediator mediator,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
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
    [Authorize]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetNotification(string id)
    {
        var result = await _notificationService.GetNotificationByIdAsync(id);

        if (!result.IsSuccess)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Value);
    }
    [Authorize]
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserNotifications(
        string userId,
        [FromQuery] int pageSize = 20,
        [FromQuery] int page = 1)
    {
        var request = new GetNotificationsRequest(userId, pageSize, page);
        var result = await _notificationService.GetUserNotificationsAsync(request);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [Authorize]
    [HttpGet("user/{userId}/unread")]
    public async Task<IActionResult> GetUnreadNotifications(string userId)
    {
        var result = await _notificationService.GetUnreadNotificationsAsync(userId);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [Authorize]
    [HttpGet("user/{userId}/unread-count")]
    public async Task<IActionResult> GetUnreadCount(string userId)
    {
        var result = await _notificationService.GetUnreadCountAsync(userId);

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

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(string id)
    {
        var result = await _notificationService.DeleteNotificationAsync(id);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }
}
