namespace PixPro.Services.Projects.Infrastructure.Messaging;

public sealed record ImageProcessingMessage(Guid ImageId, Guid OwnerId);
