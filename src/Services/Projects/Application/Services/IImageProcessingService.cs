namespace PixPro.Services.Projects.Application.Services;

public interface IImageProcessingService
{
    Task ProcessAsync(Guid imageId, CancellationToken cancellationToken);
}
