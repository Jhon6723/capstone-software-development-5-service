using Microsoft.Extensions.Logging;
using PixPro.Services.Projects.Application.Ports;
using PixPro.Services.Projects.Domain.Repositories;

namespace PixPro.Services.Projects.Application.Services;

public class ImageProcessingService : IImageProcessingService
{
    private readonly IImageRepository _imageRepository;
    private readonly IImageProcessor _imageProcessor;
    private readonly ILogger<ImageProcessingService> _logger;

    public ImageProcessingService(
        IImageRepository imageRepository,
        IImageProcessor imageProcessor,
        ILogger<ImageProcessingService> logger)
    {
        _imageRepository = imageRepository;
        _imageProcessor = imageProcessor;
        _logger = logger;
    }

    public async Task ProcessAsync(Guid imageId, CancellationToken cancellationToken)
    {
        var image = await _imageRepository.GetByIdAsync(imageId, cancellationToken);

        if (image is null)
        {
            _logger.LogWarning("Image {ImageId} not found :( so  skipping processing", imageId);
            return;
        }

        try
        {
            image.MarkAsProcessing();
            await _imageRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Image {ImageId} marked as Processing :)", imageId);

            var result = await _imageProcessor.ProcessImageAsync(image.FilePath, cancellationToken);

            image.MarkAsProcessed();
            await _imageRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Image {ImageId} processed successfully :) Result: {Result} ;)", imageId, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image {ImageId} :(", imageId);

            image.MarkAsFailed();
            await _imageRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
