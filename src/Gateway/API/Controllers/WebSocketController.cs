using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Real-time WebSocket endpoints (proxied to Notifications service)
/// </summary>
[Authorize]
[ApiController]
[Route("api/websocket")]
[Produces("application/json")]
public class WebSocketController : ControllerBase
{
    /// <summary>
    /// Establish WebSocket connection for real-time notifications
    /// </summary>
    /// <remarks>
    /// Upgrades HTTP connection to WebSocket for receiving real-time notifications.
    /// 
    /// Authentication can be provided via:
    /// - Authorization header: Bearer {token}
    /// - Query string: ?access_token={token}
    /// 
    /// Once connected, the client will receive notifications as JSON messages.
    /// 
    /// Example message:
    /// ```json
    /// {
    ///   "id": "notification-id",
    ///   "userId": "user-id",
    ///   "type": "ProjectCreated",
    ///   "title": "New Project",
    ///   "message": "Project 'MyProject' was created",
    ///   "createdAt": "2026-05-11T10:30:00Z",
    ///   "isRead": false
    /// }
    /// ```
    /// 
    /// Keep-alive interval: 2 minutes
    /// </remarks>
    [HttpGet("connect")]
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = false)]
    [ProducesResponseType(StatusCodes.Status101SwitchingProtocols)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult ConnectWebSocket()
    {
        throw new NotImplementedException("This endpoint is proxied by YARP");
    }

    /// <summary>
    /// Get WebSocket service status
    /// </summary>
    /// <remarks>
    /// Returns health information and active connection count.
    /// </remarks>
    [HttpGet("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetStatus()
    {
        throw new NotImplementedException("This endpoint is proxied by YARP");
    }

    /// <summary>
    /// Get banned IP addresses
    /// </summary>
    /// <remarks>
    /// Returns the list of currently banned IPs.
    /// </remarks>
    [HttpGet("banned")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetBannedIps()
    {
        throw new NotImplementedException("This endpoint is proxied by YARP");
    }

    /// <summary>
    /// Unban an IP address
    /// </summary>
    /// <remarks>
    /// Removes an IP from the WebSocket ban list.
    /// </remarks>
    [HttpDelete("banned/{ipAddress}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult UnbanIp(string ipAddress)
    {
        throw new NotImplementedException("This endpoint is proxied by YARP");
    }
}
