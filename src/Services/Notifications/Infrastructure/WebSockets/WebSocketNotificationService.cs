using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PixPro.Services.Notifications.Infrastructure.WebSockets;

public interface IWebSocketNotificationService
{
    Task HandleWebSocketAsync(WebSocket webSocket, string userId, string ipAddress);
    Task SendNotificationAsync(string userId, object notification);
    Task BroadcastNotificationAsync(object notification, List<string> userIds);
}

public class WebSocketNotificationService : IWebSocketNotificationService
{
    private const int MaxMessageSizeBytes = 4096; // 4 KB

    private readonly WebSocketConnectionManager _connectionManager;
    private readonly ILogger<WebSocketNotificationService> _logger;

    public WebSocketNotificationService(
        WebSocketConnectionManager connectionManager,
        ILogger<WebSocketNotificationService> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public async Task HandleWebSocketAsync(WebSocket webSocket, string userId, string ipAddress)
    {
        _connectionManager.AddConnection(userId, ipAddress, webSocket);
        _logger.LogInformation("[SECURITY] WebSocket connection established. UserId={UserId} IP={IP} TotalConnections={Total}",
            userId, ipAddress, _connectionManager.GetConnectionCount());

        var buffer = new byte[MaxMessageSizeBytes];

        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                Array.Clear(buffer, 0, buffer.Length);

                var result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None
                );

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closing connection",
                        CancellationToken.None
                    );
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    // Validate message size
                    if (result.Count > MaxMessageSizeBytes)
                    {
                        _logger.LogWarning("[SECURITY] Oversized message rejected. UserId={UserId} IP={IP} Size={Size}bytes",
                            userId, ipAddress, result.Count);
                        await webSocket.CloseAsync(WebSocketCloseStatus.MessageTooBig, "Message too large", CancellationToken.None);
                        break;
                    }

                    // Check rate limit
                    if (_connectionManager.IsRateLimited(userId))
                    {
                        _logger.LogWarning("[SECURITY] Rate limit exceeded. UserId={UserId} IP={IP} Limit={Limit}msg/min",
                            userId, ipAddress, WebSocketConnectionManager.MaxMessagesPerMinute);
                        await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Rate limit exceeded", CancellationToken.None);
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    // Validate JSON format
                    if (!IsValidJson(message))
                    {
                        _logger.LogWarning("[SECURITY] Invalid JSON message rejected. UserId={UserId} IP={IP}",
                            userId, ipAddress);
                        await SendMessageAsync(webSocket, new { type = "error", message = "Invalid message format. Expected JSON." });
                        continue;
                    }

                    _logger.LogInformation("Received message from UserId={UserId}", userId);

                    // Handle ping/pong
                    if (message.Contains("ping", StringComparison.OrdinalIgnoreCase))
                    {
                        await SendMessageAsync(webSocket, new { type = "pong", timestamp = DateTime.UtcNow });
                        _logger.LogInformation("Pong sent to UserId={UserId}", userId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SECURITY] Error in WebSocket connection. UserId={UserId} IP={IP}", userId, ipAddress);
        }
        finally
        {
            _connectionManager.RemoveConnection(userId, ipAddress);
            _logger.LogInformation("[SECURITY] WebSocket connection closed. UserId={UserId} IP={IP} TotalConnections={Total}",
                userId, ipAddress, _connectionManager.GetConnectionCount());

            if (webSocket.State != WebSocketState.Closed)
            {
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.InternalServerError,
                    "Connection terminated",
                    CancellationToken.None
                );
            }
        }
    }

    private static bool IsValidJson(string input)
    {
        try
        {
            using var doc = JsonDocument.Parse(input);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public async Task SendNotificationAsync(string userId, object notification)
    {
        var socket = _connectionManager.GetConnection(userId);
        
        if (socket != null && socket.State == WebSocketState.Open)
        {
            await SendMessageAsync(socket, notification);
            _logger.LogInformation($"Notification sent to user {userId}");
        }
        else
        {
            _logger.LogWarning($"User {userId} is not connected via WebSocket");
        }
    }

    public async Task BroadcastNotificationAsync(object notification, List<string> userIds)
    {
        var tasks = userIds
            .Where(userId => _connectionManager.IsConnected(userId))
            .Select(userId => SendNotificationAsync(userId, notification));

        await Task.WhenAll(tasks);
        _logger.LogInformation($"Notification broadcasted to {userIds.Count} users");
    }

    private async Task SendMessageAsync(WebSocket socket, object message)
    {
        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        var arraySegment = new ArraySegment<byte>(bytes, 0, bytes.Length);

        await socket.SendAsync(
            arraySegment,
            WebSocketMessageType.Text,
            true,
            CancellationToken.None
        );
    }
}
