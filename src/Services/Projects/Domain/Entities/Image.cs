namespace PixPro.Services.Projects.Domain.Entities;

public sealed class Image
{
    public Guid Id { get; private set; }
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
        Guid ownerId)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("FileName required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(cloudinaryPublicId))
            throw new ArgumentException("CloudinaryPublicId required.", nameof(cloudinaryPublicId));
        if (ownerId == Guid.Empty)
            throw new ArgumentException("OwnerId required.", nameof(ownerId));

        return new Image
        {
            Id = Guid.NewGuid(),
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
