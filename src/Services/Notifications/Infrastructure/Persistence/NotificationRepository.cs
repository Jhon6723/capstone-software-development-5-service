using MongoDB.Driver;
using PixPro.Services.Notifications.Domain.Entities;
using PixPro.Services.Notifications.Domain.Repositories;

namespace PixPro.Services.Notifications.Infrastructure.Persistence;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _context;

    public NotificationRepository(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task<Notification> CreateAsync(Notification notification)
    {
        await _context.Notifications.InsertOneAsync(notification);
        return notification;
    }

    public async Task<Notification?> GetByIdAsync(string id)
    {
        return await _context.Notifications
            .Find(n => n.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Notification>> GetByUserIdAsync(string userId, int pageSize, int page)
    {
        return await _context.Notifications
            .Find(n => n.UserId == userId)
            .SortByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
    }

    public async Task<List<Notification>> GetUnreadByUserIdAsync(string userId)
    {
        return await _context.Notifications
            .Find(n => n.UserId == userId && !n.IsRead)
            .SortByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> MarkAsReadAsync(string notificationId)
    {
        var update = Builders<Notification>.Update.Set(n => n.IsRead, true);
        var result = await _context.Notifications.UpdateOneAsync(
            n => n.Id == notificationId,
            update
        );

        return result.ModifiedCount > 0;
    }

    public async Task<bool> MarkAllAsReadAsync(string userId)
    {
        var update = Builders<Notification>.Update.Set(n => n.IsRead, true);
        var result = await _context.Notifications.UpdateManyAsync(
            n => n.UserId == userId && !n.IsRead,
            update
        );

        return result.ModifiedCount > 0;
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        var count = await _context.Notifications
            .CountDocumentsAsync(n => n.UserId == userId && !n.IsRead);

        return (int)count;
    }

    public async Task<bool> DeleteAsync(string notificationId)
    {
        var result = await _context.Notifications.DeleteOneAsync(n => n.Id == notificationId);
        return result.DeletedCount > 0;
    }
}
