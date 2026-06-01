using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Microsoft.AspNetCore.Http;

namespace API.Models;

/// <summary>
/// AI processing feature type
/// </summary>
public enum ProcessingFeature
{
    /// <summary>
    /// Text-to-image generation (no source image required)
    /// </summary>
    Generator = 0,

    /// <summary>
    /// Image-to-image editing (requires source image)
    /// </summary>
    Editor = 1
}

/// <summary>
/// Request model for uploading an image or generating from text
/// </summary>
public class UploadImageRequest
{
    /// <summary>
    /// Image file to upload (optional for text-to-image, required for image-to-image)
    /// Max 10MB. Accepted formats: .jpg, .jpeg, .png, .webp
    /// </summary>
    public IFormFile? File { get; set; }

    /// <summary>
    /// Project ID to associate the image with (required)
    /// </summary>
    /// <example>550e8400-e29b-41d4-a716-446655440000</example>
    [Required]
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Text prompt describing the desired image or transformation (required)
    /// </summary>
    /// <example>Transform this image into a cyberpunk style with neon lights</example>
    [Required]
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// Processing feature: 0=Generator (text-to-image), 1=Editor (image-to-image) (required)
    /// </summary>
    /// <example>0</example>
    [Required]
    public int Feature { get; set; }

    /// <summary>
    /// Processing parameters as JSON string (optional for Generator, required for Editor)
    /// </summary>
    /// <remarks>
    /// <b>Generator mode (Feature=0):</b> Optional. Only basic parameters like width/height used.
    /// <b>Editor mode (Feature=1):</b> Required. Must include 'model' field.
    /// </remarks>
    /// <example>Generator: {"width": 512, "height": 512, "quantity": 1}</example>
    /// <example>Editor: {"width": 512, "height": 512, "model": "gpt-image-1-mini-low", "strength": 0.75}</example>
    public string? Parameters { get; set; }
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
/// Image upload response
/// </summary>
public class ImageResponse
{
    /// <summary>
    /// Image unique identifier
    /// </summary>
    public string ImageId { get; set; } = string.Empty;

    /// <summary>
    /// Original file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// URL to access the image
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Secure HTTPS URL to the image
    /// </summary>
    public string SecureUrl { get; set; } = string.Empty;

    /// <summary>
    /// Image format (jpg, png, webp, etc.)
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long SizeInBytes { get; set; }

    /// <summary>
    /// Image width in pixels
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Image height in pixels
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Upload timestamp
    /// </summary>
    public DateTimeOffset UploadedAt { get; set; }

    /// <summary>
    /// Processing feature used: 0=Generator (text-to-image), 1=Editor (image-to-image)
    /// </summary>
    /// <example>0</example>
    public int Feature { get; set; }
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
