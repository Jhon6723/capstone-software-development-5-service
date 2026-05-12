namespace PixPro.Services.Projects.Application.IntegrationEvents;

public record ImageUploadedEvent(Guid ImageId, Guid OwnerId);
