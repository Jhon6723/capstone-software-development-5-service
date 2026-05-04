using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace PixPro.Services.Notifications.Infrastructure.WebSockets;

public class WebSocketConnectionManager
{
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();

    public void AddConnection(string userId, WebSocket webSocket)
    {
        _connections.TryAdd(userId, webSocket);
    }

    public bool RemoveConnection(string userId)
    {
        return _connections.TryRemove(userId, out _);
    }

    public WebSocket? GetConnection(string userId)
    {
        _connections.TryGetValue(userId, out var webSocket);
        return webSocket;
    }

    public IEnumerable<string> GetAllConnectionIds()
    {
        return _connections.Keys;
    }

    public int GetConnectionCount()
    {
        return _connections.Count;
    }

    public bool IsConnected(string userId)
    {
        return _connections.ContainsKey(userId) && 
               _connections.TryGetValue(userId, out var socket) &&
               socket?.State == WebSocketState.Open;
    }
}
