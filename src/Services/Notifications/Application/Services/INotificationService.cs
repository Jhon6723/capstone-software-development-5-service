using PixPro.Services.Notifications.Application.Common.Results;
using PixPro.Services.Notifications.Application.DTOs.Requests;
using PixPro.Services.Notifications.Application.DTOs.Responses;

namespace PixPro.Services.Notifications.Application.Services;

public interface INotificationService
{
    Task<Result<NotificationResponse>> CreateNotificationAsync(CreateNotificationRequest request);
    Task<Result<NotificationResponse>> GetNotificationByIdAsync(string id);
    Task<Result<NotificationListResponse>> GetUserNotificationsAsync(GetNotificationsRequest request);
    Task<Result<List<NotificationResponse>>> GetUnreadNotificationsAsync(string userId);
    Task<Result> MarkAsReadAsync(string notificationId);
    Task<Result> MarkAllAsReadAsync(string userId);
    Task<Result<int>> GetUnreadCountAsync(string userId);
    Task<Result> DeleteNotificationAsync(string notificationId);
}
