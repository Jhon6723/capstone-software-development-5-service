using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixPro.Services.Notifications.Infrastructure.WebSockets;

namespace PixPro.Services.Notifications.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebSocketController : ControllerBase
{
    private readonly IWebSocketNotificationService _webSocketService;
    private readonly ILogger<WebSocketController> _logger;

    public WebSocketController(
        IWebSocketNotificationService webSocketService,
        ILogger<WebSocketController> logger)
    {
        _webSocketService = webSocketService;
        _logger = logger;
    }

    /// <summary>
    /// Establishes an authenticated WebSocket connection to receive real-time notifications
    /// </summary>
    /// <remarks>
    /// To connect via JavaScript:
    /// ```javascript
    /// const token = "your-jwt-token";
    /// const ws = new WebSocket(`ws://localhost:8083/api/websocket/connect?access_token=${token}`);
    /// ```
    /// </remarks>
    [HttpGet("connect")]
    [Authorize]
    public async Task Connect()
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsync("Expected a WebSocket request");
            return;
        }

        // Extraer userId del token JWT validado
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) 
                       ?? User.FindFirst("sub");
        
        if (userIdClaim == null)
        {
            HttpContext.Response.StatusCode = 401;
            await HttpContext.Response.WriteAsync("Invalid token: user ID not found");
            return;
        }

        var userId = userIdClaim.Value;
        _logger.LogInformation($"WebSocket connection authenticated for user: {userId}");
        
        var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        await _webSocketService.HandleWebSocketAsync(webSocket, userId);
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            status = "WebSocket service is running",
            timestamp = DateTime.UtcNow
        });
    }
}
