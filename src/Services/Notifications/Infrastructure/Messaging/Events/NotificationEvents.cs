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

public record ImageProcessingCompletedEvent(
    string ImageId,
    string UserId,
    string ImageUrl,
    string ProcessedImageUrl,
    Dictionary<string, string> ProcessingResults,
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
