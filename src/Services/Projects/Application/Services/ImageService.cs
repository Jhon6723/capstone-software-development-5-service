using Microsoft.Extensions.Logging;
using PixPro.Services.Projects.Application.Common.Results;
using PixPro.Services.Projects.Application.DTOs.Requests;
using PixPro.Services.Projects.Application.DTOs.Responses;
using PixPro.Services.Projects.Application.IntegrationEvents;
using PixPro.Services.Projects.Application.Interfaces;
using PixPro.Services.Projects.Domain.Entities;
using PixPro.Services.Projects.Domain.Repositories;

namespace PixPro.Services.Projects.Application.Services;

public class ImageService : IImageService
{
    private readonly IImageRepository _imageRepository;
    private readonly IStorageService _storageService;
    private readonly ILogger<ImageService> _logger;
    private readonly IMessagePublisher _messagePublisher;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public ImageService(
        IImageRepository imageRepository,
        IStorageService storageService,
        ILogger<ImageService> logger,
        IMessagePublisher messagePublisher)
    {
        _imageRepository = imageRepository;
        _storageService = storageService;
        _logger = logger;
        _messagePublisher = messagePublisher;
    }

    public async Task<Result<ImageUploadResponse>> UploadAsync(
        UploadImageRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var file = request.File;

            if (file is null || file.Length == 0)
                return Result<ImageUploadResponse>.Failure("No file provided.");

            if (file.Length > MaxFileSizeBytes)
                return Result<ImageUploadResponse>.Failure("File exceeds the 5 MB size limit.");

            var extension = Path.GetExtension(file.FileName);
            if (!AllowedExtensions.Contains(extension))
                return Result<ImageUploadResponse>.Failure(
                    $"Extension '{extension}' is not allowed. Use: .jpg, .jpeg, .png, .webp");

            if (!AllowedContentTypes.Contains(file.ContentType))
                return Result<ImageUploadResponse>.Failure(
                    $"Content-Type '{file.ContentType}' is not allowed.");

            using var stream = file.OpenReadStream();

            var uploaded = await _storageService.UploadAsync(
                stream,
                file.FileName,
                file.ContentType,
                cancellationToken);

            var image = Image.CreateFromCloudinary(
                file.FileName,
                file.ContentType,
                uploaded.PublicId,
                uploaded.Url,
                uploaded.SecureUrl,
                uploaded.Format,
                uploaded.Bytes,
                uploaded.Width,
                uploaded.Height,
                request.OwnerId);

            await _imageRepository.AddAsync(image, cancellationToken);
            await _imageRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Image uploaded: {ImageId} by Owner: {OwnerId}", image.Id, request.OwnerId);

            try
            {
                await _messagePublisher.PublishAsync(
                    new ImageUploadedEvent(image.Id, request.OwnerId, image.SecureUrl),
                    "image-processing-events",
                    cancellationToken);

                _logger.LogInformation("ImageUploadedEvent published for ImageId: {ImageId}", image.Id);
            }
            catch (Exception pubEx)
            {
                _logger.LogWarning(pubEx, "failed to publish ImageUploadedEvent for ImageId: {ImageId}. Image was saved successfully", image.Id);
            }

            return Result<ImageUploadResponse>.Success(new ImageUploadResponse(
                image.Id,
                image.FileName,
                image.FilePath,
                image.SecureUrl,
                image.Format,
                image.SizeInBytes,
                image.Width,
                image.Height,
                image.CreatedAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image for Owner: {OwnerId}", request.OwnerId);
            return Result<ImageUploadResponse>.Failure($"Error uploading image: {ex.Message}");
        }
    }
}
