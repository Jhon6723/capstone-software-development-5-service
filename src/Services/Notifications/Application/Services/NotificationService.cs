using PixPro.Services.Notifications.Application.Common.Results;
using PixPro.Services.Notifications.Application.DTOs.Requests;
using PixPro.Services.Notifications.Application.DTOs.Responses;
using PixPro.Services.Notifications.Domain.Entities;
using PixPro.Services.Notifications.Domain.Repositories;

namespace PixPro.Services.Notifications.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationService(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<Result<NotificationResponse>> CreateNotificationAsync(CreateNotificationRequest request)
    {
        try
        {
            var notification = new Notification
            {
                UserId = request.UserId,
                Type = request.Type,
                Title = request.Title,
                Message = request.Message,
                Metadata = request.Metadata
            };

            var createdNotification = await _notificationRepository.CreateAsync(notification);
            var response = MapToResponse(createdNotification);

            return Result<NotificationResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<NotificationResponse>.Failure($"Error creating notification: {ex.Message}");
        }
    }

    public async Task<Result<NotificationResponse>> GetNotificationByIdAsync(string id)
    {
        try
        {
            var notification = await _notificationRepository.GetByIdAsync(id);
            
            if (notification == null)
            {
                return Result<NotificationResponse>.Failure("Notification not found");
            }

            var response = MapToResponse(notification);
            return Result<NotificationResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<NotificationResponse>.Failure($"Error retrieving notification: {ex.Message}");
        }
    }

    public async Task<Result<NotificationListResponse>> GetUserNotificationsAsync(GetNotificationsRequest request)
    {
        try
        {
            var notifications = await _notificationRepository.GetByUserIdAsync(
                request.UserId, 
                request.PageSize, 
                request.Page
            );

            var responses = notifications.Select(MapToResponse).ToList();
            var totalCount = await _notificationRepository.GetUnreadCountAsync(request.UserId);

            var response = new NotificationListResponse(
                responses,
                totalCount,
                request.Page,
                request.PageSize
            );

            return Result<NotificationListResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<NotificationListResponse>.Failure($"Error retrieving notifications: {ex.Message}");
        }
    }

    public async Task<Result<List<NotificationResponse>>> GetUnreadNotificationsAsync(string userId)
    {
        try
        {
            var notifications = await _notificationRepository.GetUnreadByUserIdAsync(userId);
            var responses = notifications.Select(MapToResponse).ToList();

            return Result<List<NotificationResponse>>.Success(responses);
        }
        catch (Exception ex)
        {
            return Result<List<NotificationResponse>>.Failure($"Error retrieving unread notifications: {ex.Message}");
        }
    }

    public async Task<Result> MarkAsReadAsync(string notificationId)
    {
        try
        {
            var success = await _notificationRepository.MarkAsReadAsync(notificationId);
            
            if (!success)
            {
                return Result.Failure("Failed to mark notification as read");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error marking notification as read: {ex.Message}");
        }
    }

    public async Task<Result> MarkAllAsReadAsync(string userId)
    {
        try
        {
            var success = await _notificationRepository.MarkAllAsReadAsync(userId);
            
            if (!success)
            {
                return Result.Failure("Failed to mark all notifications as read");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error marking all notifications as read: {ex.Message}");
        }
    }

    public async Task<Result<int>> GetUnreadCountAsync(string userId)
    {
        try
        {
            var count = await _notificationRepository.GetUnreadCountAsync(userId);
            return Result<int>.Success(count);
        }
        catch (Exception ex)
        {
            return Result<int>.Failure($"Error getting unread count: {ex.Message}");
        }
    }

    public async Task<Result> DeleteNotificationAsync(string notificationId)
    {
        try
        {
            var success = await _notificationRepository.DeleteAsync(notificationId);
            
            if (!success)
            {
                return Result.Failure("Failed to delete notification");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error deleting notification: {ex.Message}");
        }
    }

    private static NotificationResponse MapToResponse(Notification notification)
    {
        return new NotificationResponse(
            notification.Id,
            notification.UserId,
            notification.Type,
            notification.Title,
            notification.Message,
            notification.IsRead,
            notification.CreatedAt,
            notification.Metadata
        );
    }
}
