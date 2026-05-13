using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Microsoft.AspNetCore.Http;

namespace API.Models;

/// <summary>
/// Request model for uploading an image
/// </summary>
public class UploadImageRequest
{
    /// <summary>
    /// Image file to upload (max 10MB)
    /// </summary>
    [Required]
    public required IFormFile File { get; set; }
}

/// <summary>
/// Image processing status
/// </summary>
public enum ImageStatus
{
    /// <summary>
    /// Uploaded, waiting for processing
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Currently being processed
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Processing completed successfully
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Processing failed
    /// </summary>
    Failed = 3
}

/// <summary>
/// Request model for updating image metadata
/// </summary>
public class UpdateImageRequest
{
    /// <summary>
    /// Image title
    /// </summary>
    [DefaultValue("Beach Sunset")]
    public string? Title { get; set; }

    /// <summary>
    /// Image description
    /// </summary>
    [DefaultValue("Beautiful sunset at the beach")]
    public string? Description { get; set; }

    /// <summary>
    /// Image tags for categorization
    /// </summary>
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Image information
/// </summary>
public class ImageResponse
{
    /// <summary>
    /// Image unique identifier
    /// </summary>
    public required string ImageId { get; set; }
}

/// <summary>
/// Paginated list of images
/// </summary>
public class ImageListResponse
{
    /// <summary>
    /// List of images
    /// </summary>
    public required List<ImageResponse> Images { get; set; }

    /// <summary>
    /// Total count of images
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }
}

/// <summary>
/// Image processing status response
/// </summary>
public class ImageStatusResponse
{
    /// <summary>
    /// Image ID
    /// </summary>
    public required string ImageId { get; set; }

    /// <summary>
    /// Processing status
    /// </summary>
    public ImageStatus Status { get; set; }

    /// <summary>
    /// Processing progress (0-100)
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Detected features or objects
    /// </summary>
    public Dictionary<string, object>? Results { get; set; }
}
