using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixPro.Services.Notifications.API.Helpers;
using PixPro.Services.Notifications.Infrastructure.WebSockets;

namespace PixPro.Services.Notifications.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebSocketController : ControllerBase
{
    private readonly IWebSocketNotificationService _webSocketService;
    private readonly WebSocketConnectionManager _connectionManager;
    private readonly ILogger<WebSocketController> _logger;

    public WebSocketController(
        IWebSocketNotificationService webSocketService,
        WebSocketConnectionManager connectionManager,
        ILogger<WebSocketController> logger)
    {
        _webSocketService = webSocketService;
        _connectionManager = connectionManager;
        _logger = logger;
    }

    [HttpGet("connect")]
    [Authorize]
    public async Task Connect()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            _logger.LogWarning("[SECURITY] Non-WebSocket request rejected. IP={IP}", ipAddress);
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsync("Expected a WebSocket request");
            return;
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                       ?? User.FindFirst("sub");

        if (userIdClaim == null)
        {
            _logger.LogWarning("[SECURITY] Authentication failure: user ID not found in token. IP={IP}", ipAddress);
            HttpContext.Response.StatusCode = 401;
            await HttpContext.Response.WriteAsync("Invalid token: user ID not found");
            return;
        }

        var userId = UserIdHelper.DeriveUserId(userIdClaim.Value);

        // Check connection limits
        if (!_connectionManager.CanConnect(ipAddress))
        {
            _logger.LogWarning("[SECURITY] Connection limit exceeded. UserId={UserId} IP={IP} Total={Total} Limit={Limit}",
                userId, ipAddress, _connectionManager.GetConnectionCount(), WebSocketConnectionManager.MaxTotalConnections);
            HttpContext.Response.StatusCode = 429;
            await HttpContext.Response.WriteAsync("Connection limit exceeded");
            return;
        }

        _logger.LogInformation("[SECURITY] WebSocket connection attempt. UserId={UserId} IP={IP}", userId, ipAddress);

        var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        await _webSocketService.HandleWebSocketAsync(webSocket, userId, ipAddress);
    }

    [HttpGet("status")]
    [Authorize]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            status = "WebSocket service is running",
            activeConnections = _connectionManager.GetConnectionCount(),
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("banned")]
    [Authorize(Policy = "Admin")]
    public IActionResult GetBannedIps()
    {
        var banned = _connectionManager.GetBannedIps();
        return Ok(new
        {
            bannedIps = banned,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpDelete("banned/{ipAddress}")]
    [Authorize(Policy = "Admin")]
    public IActionResult UnbanIp(string ipAddress)
    {
        var unbanned = _connectionManager.UnbanIp(ipAddress);
        if (!unbanned)
            return NotFound(new { message = $"IP '{ipAddress}' is not banned" });

        _logger.LogInformation("[SECURITY] IP manually unbanned. IP={IP}", ipAddress);
        return Ok(new { message = $"IP '{ipAddress}' has been unbanned" });
    }
}
