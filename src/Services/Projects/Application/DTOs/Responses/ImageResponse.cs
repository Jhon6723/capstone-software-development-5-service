namespace PixPro.Services.Projects.Application.DTOs.Responses;

public class ImageResponse
{
    public required string Id { get; set; }
    public required string ProjectId { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required string FilePath { get; set; }
    public required string CloudinaryPublicId { get; set; }
    public required string SecureUrl { get; set; }
    public required string Format { get; set; }
    public long SizeInBytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public required string Status { get; set; }
    public required string OwnerId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
