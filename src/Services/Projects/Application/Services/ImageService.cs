using Microsoft.Extensions.Logging;
using PixPro.Services.Projects.Application.Common.Results;
using PixPro.Services.Projects.Application.DTOs.Requests;
using PixPro.Services.Projects.Application.DTOs.Responses;
using PixPro.Services.Projects.Application.IntegrationEvents;
using PixPro.Services.Projects.Application.Interfaces;
using PixPro.Services.Projects.Domain.Entities;
using PixPro.Services.Projects.Domain.Enums;
using PixPro.Services.Projects.Domain.Repositories;

namespace PixPro.Services.Projects.Application.Services;

public class ImageService : IImageService
{
    private readonly IImageRepository _imageRepository;
    private readonly IStorageService _storageService;
    private readonly ILogger<ImageService> _logger;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ICreditService _creditService;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, ModelTier> ModelTierMap =
        new Dictionary<string, ModelTier>(StringComparer.OrdinalIgnoreCase)
        {
            { "gpt-image-1-mini-low",  ModelTier.GptMiniLow  },
            { "gpt-image-1-mini-high", ModelTier.GptMiniHigh },
            { "kontext",               ModelTier.Kontext      }
        };

    public ImageService(
        IImageRepository imageRepository,
        IStorageService storageService,
        ILogger<ImageService> logger,
        IMessagePublisher messagePublisher,
        ICreditService creditService)
    {
        _imageRepository = imageRepository;
        _storageService = storageService;
        _logger = logger;
        _messagePublisher = messagePublisher;
        _creditService = creditService;
    }

    public async Task<Result<ImageUploadResponse>> UploadAsync(
        UploadImageRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Credit check for non-admin users on img2img models
            if (!request.IsAdmin && request.Parameters is not null
                && ModelTierMap.TryGetValue(request.Parameters.Model, out var modelTier))
            {
                var deductResult = await _creditService.TryDeductCreditAsync(
                    request.OwnerId, modelTier, cancellationToken);

                if (!deductResult.IsSuccess)
                    return Result<ImageUploadResponse>.Failure(deductResult.Error);
            }
            var file = request.File;
            string? imageUrl = null;
            Image? image = null;

            // Handle file upload if provided (image-to-image)
            if (file is not null && file.Length > 0)
            {
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

                image = Image.CreateFromCloudinary(
                    file.FileName,
                    file.ContentType,
                    uploaded.PublicId,
                    uploaded.Url,
                    uploaded.SecureUrl,
                    uploaded.Format,
                    uploaded.Bytes,
                    uploaded.Width,
                    uploaded.Height,
                    request.OwnerId,
                    request.ProjectId);

                await _imageRepository.AddAsync(image, cancellationToken);
                await _imageRepository.SaveChangesAsync(cancellationToken);

                imageUrl = image.SecureUrl;

                _logger.LogInformation("Image uploaded: {ImageId} by Owner: {OwnerId}", image.Id, request.OwnerId);
            }

            // Generate a new ImageId for text-to-image if no file was uploaded
            var imageId = image?.Id ?? Guid.NewGuid();

            // Use provided feature or auto-detect based on file presence
            var feature = request.Feature ?? (file != null ? ProcessingFeature.Editor : ProcessingFeature.Generator);

            var featureName = feature == ProcessingFeature.Generator ? "Generator" : "Editor";
            _logger.LogInformation(
                "Processing {ProcessingType} for Owner: {OwnerId}, Feature: {FeatureName}",
                file != null ? "image-to-image" : "text-to-image",
                request.OwnerId,
                featureName);

            try
            {
                var integrationEvent = new ImageUploadedEvent(
                    imageId,
                    request.OwnerId,
                    request.ProjectId,
                    imageUrl,
                    request.Prompt,
                    feature,
                    request.Parameters);

                await _messagePublisher.PublishAsync(
                    integrationEvent,
                    "image-events",
                    cancellationToken);

                _logger.LogInformation(
                    "ImageUploadedEvent published for ImageId: {ImageId}, Type: {ProcessingType}",
                    imageId,
                    file != null ? "image-to-image" : "text-to-image");
            }
            catch (Exception pubEx)
            {
                _logger.LogWarning(pubEx,
                    "Failed to publish ImageUploadedEvent for ImageId: {ImageId}. Request was processed successfully",
                    imageId);
            }

            return Result<ImageUploadResponse>.Success(new ImageUploadResponse(
                imageId,
                image?.FileName ?? "",
                image?.FilePath ?? "",
                image?.SecureUrl ?? "",
                image?.Format ?? "",
                image?.SizeInBytes ?? 0,
                image?.Width ?? 0,
                image?.Height ?? 0,
                image?.CreatedAt ?? DateTimeOffset.UtcNow,
                feature));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image request for Owner: {OwnerId}", request.OwnerId);
            return Result<ImageUploadResponse>.Failure($"Error processing image request: {ex.Message}");
        }
    }
}
