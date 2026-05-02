using Microsoft.AspNetCore.Mvc;
using PixPro.Services.Projects.Domain.Entities;
using PixPro.Services.Projects.Domain.Repositories;

namespace PixPro.Services.Projects.API.Controllers;

[ApiController]
[Route("api/images")]
[Produces("application/json")]
public class ImagesController : ControllerBase
{
    private readonly IImageRepository _imageRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ImagesController> _logger;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public ImagesController(
        IImageRepository imageRepository,
        IConfiguration configuration,
        ILogger<ImagesController> logger)
    {
        _imageRepository = imageRepository;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        if (file.Length > MaxFileSizeBytes)
            return BadRequest(new { error = "File exceeds the 5 MB size limit." });

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
            return BadRequest(new { error = $"Extension '{extension}' is not allowed. Use: .jpg, .jpeg, .png, .webp" });

        if (!AllowedContentTypes.Contains(file.ContentType))
            return BadRequest(new { error = $"Content-Type '{file.ContentType}' is not allowed." });

        
        var ownerId = Guid.TryParse(Request.Headers["X-User-Id"].FirstOrDefault(), out var parsed)
            ? parsed
            : Guid.Parse("00000000-0000-0000-0000-000000000001");

        var basePath = _configuration["Storage:LocalPath"] ?? "uploads/images";
        Directory.CreateDirectory(basePath);

        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(basePath, uniqueFileName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var image = new Image(file.FileName, file.ContentType, fullPath, ownerId);

        await _imageRepository.AddAsync(image, cancellationToken);
        await _imageRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Image uploaded: {ImageId} by Owner: {OwnerId}", image.Id, ownerId);

        return StatusCode(StatusCodes.Status201Created, new { imageId = image.Id });
    }
}
