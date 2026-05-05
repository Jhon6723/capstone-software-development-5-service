namespace PixPro.Services.Projects.Domain.Entities;

public sealed class Image
{
    public Guid Id { get; private set; }
    public string FileName { get; private set; }
    public string ContentType { get; private set; }
    public string FilePath { get; private set; }
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
        Status = "Pending";
        OwnerId = ownerId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    private Image()
    {
        FileName = string.Empty;
        ContentType = string.Empty;
        FilePath = string.Empty;
        Status = string.Empty;
    }
}
