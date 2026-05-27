using System.Text.Json;
using PixPro.Services.Notifications.Domain.Enums;

namespace PixPro.Services.Notifications.Infrastructure.Messaging.Events;

public record UserRegisteredEvent(
    string UserId,
    string Email,
    string Name
);

public record ProjectCreatedEvent(
    string ProjectId,
    string ProjectName,
    string CreatorId,
    List<string> TeamMemberIds
);

public record ProjectUpdatedEvent(
    string ProjectId,
    string ProjectName,
    string UpdatedBy,
    List<string> StakeholderIds
);

public record ProjectAssignedEvent(
    string ProjectId,
    string ProjectName,
    string AssignedUserId,
    string AssignedBy
);

public record NotificationEvent(
    string UserId,
    NotificationType Type,
    string Title,
    string Message,
    Dictionary<string, string>? Metadata = null
);

public record ImageUploadedEvent(
    string ImageId,
    string OwnerId,
    string Prompt,
    int Feature,
    string? ImageUrl = null,
    JsonElement? Parameters = null
);

public record ImageProcessingCompletedEvent(
    string ImageId,
    string UserId,
    string ImageUrl,
    List<string> ProcessedImageUrls,
    JsonElement? ProcessingResults,
    DateTime CompletedAt
);

public record ImageProcessingFailedEvent(
    string ImageId,
    string UserId,
    string ImageUrl,
    string ErrorMessage,
    string ErrorCode,
    DateTime FailedAt
);
