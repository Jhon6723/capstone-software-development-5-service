using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PixPro.Services.Notifications.Infrastructure.WebSockets;

public interface IWebSocketNotificationService
{
    Task HandleWebSocketAsync(WebSocket webSocket, string userId);
    Task SendNotificationAsync(string userId, object notification);
    Task BroadcastNotificationAsync(object notification, List<string> userIds);
}

public class WebSocketNotificationService : IWebSocketNotificationService
{
    private readonly WebSocketConnectionManager _connectionManager;
    private readonly ILogger<WebSocketNotificationService> _logger;

    public WebSocketNotificationService(
        WebSocketConnectionManager connectionManager,
        ILogger<WebSocketNotificationService> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public async Task HandleWebSocketAsync(WebSocket webSocket, string userId)
    {
        _connectionManager.AddConnection(userId, webSocket);
        _logger.LogInformation($"WebSocket connection established for user: {userId}");

        var buffer = new byte[1024 * 4];

        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                // Clear buffer before each receive
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

                // Optionally handle incoming messages from client
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _logger.LogInformation($"Received message from {userId}: {message}");
                    
                    // Handle ping/pong or other client messages
                    if (message.Contains("ping", StringComparison.OrdinalIgnoreCase))
                    {
                        await SendMessageAsync(webSocket, new { type = "pong", timestamp = DateTime.UtcNow });
                        _logger.LogInformation($"Pong sent to user {userId}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in WebSocket connection for user {userId}");
        }
        finally
        {
            _connectionManager.RemoveConnection(userId);
            _logger.LogInformation($"WebSocket connection closed for user: {userId}");
            
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
