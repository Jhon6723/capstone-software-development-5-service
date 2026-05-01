using PixPro.Services.Notifications.Domain.Entities;

namespace PixPro.Services.Notifications.Domain.Repositories;

public interface INotificationRepository
{
    Task<Notification> CreateAsync(Notification notification);
    Task<Notification?> GetByIdAsync(string id);
    Task<List<Notification>> GetByUserIdAsync(string userId, int pageSize, int page);
    Task<List<Notification>> GetUnreadByUserIdAsync(string userId);
    Task<bool> MarkAsReadAsync(string notificationId);
    Task<bool> MarkAllAsReadAsync(string userId);
    Task<int> GetUnreadCountAsync(string userId);
    Task<bool> DeleteAsync(string notificationId);
}
