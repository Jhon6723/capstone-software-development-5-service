using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixPro.Services.Notifications.Application.DTOs.Requests;
using PixPro.Services.Notifications.Application.Services;

namespace PixPro.Services.Notifications.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationRequest request)
    {
        var result = await _notificationService.CreateNotificationAsync(request);

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

    [Authorize]
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(string id)
    {
        var result = await _notificationService.MarkAsReadAsync(id);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    [Authorize]
    [HttpPut("user/{userId}/read-all")]
    public async Task<IActionResult> MarkAllAsRead(string userId)
    {
        var result = await _notificationService.MarkAllAsReadAsync(userId);

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
