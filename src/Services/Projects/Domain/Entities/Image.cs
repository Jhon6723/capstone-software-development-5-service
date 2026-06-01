namespace PixPro.Services.Projects.Domain.Entities;

public sealed class Image
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Project Project { get; private set; } = null!;
    public Guid? OriginalImageId { get; private set; }  // NULL = imagen original, valor = imagen procesada
    public string FileName { get; private set; }
    public string ContentType { get; private set; }
    public string FilePath { get; private set; }
    public string CloudinaryPublicId { get; private set; }
    public string SecureUrl { get; private set; }
    public string Format { get; private set; }
    public long SizeInBytes { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public string Status { get; private set; }
    public Guid OwnerId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Image(string fileName, string contentType, string filePath, Guid ownerId)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("FileName required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("ContentType required.", nameof(contentType));
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("FilePath required.", nameof(filePath));
        if (ownerId == Guid.Empty)
            throw new ArgumentException("OwnerId required.", nameof(ownerId));

        Id = Guid.NewGuid();
        FileName = fileName.Trim();
        ContentType = contentType.Trim();
        FilePath = filePath;
        CloudinaryPublicId = string.Empty;
        SecureUrl = string.Empty;
        Format = string.Empty;
        Status = "Pending";
        OwnerId = ownerId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static Image CreateFromCloudinary(
        string fileName,
        string contentType,
        string cloudinaryPublicId,
        string url,
        string secureUrl,
        string format,
        long sizeInBytes,
        int width,
        int height,
        Guid ownerId,
        Guid projectId)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("FileName required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(cloudinaryPublicId))
            throw new ArgumentException("CloudinaryPublicId required.", nameof(cloudinaryPublicId));
        if (ownerId == Guid.Empty)
            throw new ArgumentException("OwnerId required.", nameof(ownerId));

        if (projectId == Guid.Empty)
            throw new ArgumentException("ProjectId required.", nameof(projectId));

        return new Image
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            FileName = fileName.Trim(),
            ContentType = contentType.Trim(),
            FilePath = url,
            CloudinaryPublicId = cloudinaryPublicId,
            SecureUrl = secureUrl,
            Format = format,
            SizeInBytes = sizeInBytes,
            Width = width,
            Height = height,
            Status = "Pending",
            OwnerId = ownerId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static Image CreateProcessedImage(
        Guid? originalImageId,
        Guid projectId,
        Guid ownerId,
        string secureUrl,
        string status)
    {
        if (projectId == Guid.Empty)
            throw new ArgumentException("ProjectId required.", nameof(projectId));
        if (ownerId == Guid.Empty)
            throw new ArgumentException("OwnerId required.", nameof(ownerId));
        if (string.IsNullOrWhiteSpace(secureUrl))
            throw new ArgumentException("SecureUrl required.", nameof(secureUrl));

        // Generate unique filename
        var fileName = $"processed_{Guid.NewGuid():N}.jpg";
        var contentType = "image/jpeg";

        return new Image
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            OriginalImageId = originalImageId,  // NULL for generated images, real ID for edited images
            FileName = fileName,
            ContentType = contentType,
            FilePath = secureUrl,
            CloudinaryPublicId = string.Empty,
            SecureUrl = secureUrl,
            Format = "jpg",
            SizeInBytes = 0,
            Width = 0,
            Height = 0,
            Status = status,
            OwnerId = ownerId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private Image()
    {
        FileName = string.Empty;
        ContentType = string.Empty;
        FilePath = string.Empty;
        CloudinaryPublicId = string.Empty;
        SecureUrl = string.Empty;
        Format = string.Empty;
        Status = string.Empty;
    }
}
