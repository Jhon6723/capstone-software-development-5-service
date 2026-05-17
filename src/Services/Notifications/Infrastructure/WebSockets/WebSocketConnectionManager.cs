using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace PixPro.Services.Notifications.Infrastructure.WebSockets;

public class WebSocketConnectionManager
{
    // Limits
    public const int MaxTotalConnections = 1000;
    public const int MaxConnectionsPerIp = 5;
    public const int MaxMessagesPerMinute = 60;

    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();
    private readonly ConcurrentDictionary<string, int> _ipConnectionCounts = new();
    private readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)> _messageCounts = new();
    private readonly ConcurrentDictionary<string, DateTime> _bannedIps = new();

    private static readonly TimeSpan BanDuration = TimeSpan.FromMinutes(876600); // 1 year ban for IPs that exceed limits

    // --- Connection management ---

    public bool CanConnect(string ipAddress)
    {
        // Check if IP is banned
        if (_bannedIps.TryGetValue(ipAddress, out var bannedUntil))
        {
            if (DateTime.UtcNow < bannedUntil)
                return false;

            // Ban expired, remove it
            _bannedIps.TryRemove(ipAddress, out _);
        }

        if (_connections.Count >= MaxTotalConnections)
            return false;

        var ipCount = _ipConnectionCounts.GetOrAdd(ipAddress, 0);
        if (ipCount >= MaxConnectionsPerIp)
        {
            // Ban the IP for exceeding the per-IP limit
            _bannedIps.TryAdd(ipAddress, DateTime.UtcNow.Add(BanDuration));
            return false;
        }

        return true;
    }

    public IEnumerable<object> GetBannedIps()
    {
        var now = DateTime.UtcNow;
        // Clean expired bans and return active ones
        foreach (var kvp in _bannedIps)
        {
            if (kvp.Value <= now)
                _bannedIps.TryRemove(kvp.Key, out _);
        }

        return _bannedIps.Select(kvp => new
        {
            ip = kvp.Key,
            bannedUntil = kvp.Value,
            remainingSeconds = (int)(kvp.Value - now).TotalSeconds
        });
    }

    public bool UnbanIp(string ipAddress)
    {
        return _bannedIps.TryRemove(ipAddress, out _);
    }

    public void AddConnection(string userId, string ipAddress, WebSocket webSocket)
    {
        _connections[userId] = webSocket;
        _ipConnectionCounts.AddOrUpdate(ipAddress, 1, (_, count) => count + 1);
    }

    public bool RemoveConnection(string userId, string ipAddress)
    {
        var removed = _connections.TryRemove(userId, out _);
        if (removed)
        {
            _ipConnectionCounts.AddOrUpdate(ipAddress, 0, (_, count) => Math.Max(0, count - 1));
        }
        _messageCounts.TryRemove(userId, out _);
        return removed;
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

    // --- Rate limiting ---

    public bool IsRateLimited(string userId)
    {
        var now = DateTime.UtcNow;
        var entry = _messageCounts.GetOrAdd(userId, _ => (0, now));

        // Reset window if more than 1 minute has passed
        if ((now - entry.WindowStart).TotalMinutes >= 1)
        {
            entry = (0, now);
        }

        if (entry.Count >= MaxMessagesPerMinute)
            return true;

        _messageCounts[userId] = (entry.Count + 1, entry.WindowStart);
        return false;
    }
}
