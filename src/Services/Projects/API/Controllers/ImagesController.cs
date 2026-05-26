using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixPro.Services.Projects.Application.DTOs.Requests;
using PixPro.Services.Projects.Application.IntegrationEvents;
using PixPro.Services.Projects.Application.Services;
using System.Security.Claims;
using System.Text.Json;

namespace PixPro.Services.Projects.API.Controllers;

[ApiController]
[Route("api/images")]
[Produces("application/json")]
public class ImagesController : ControllerBase
{
    private readonly IImageService _imageService;

    public ImagesController(IImageService imageService)
    {
        _imageService = imageService;
    }

    [HttpPost("upload")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Upload(
        IFormFile? file,
        [FromForm] string prompt,
        [FromForm] int? feature = null,
        [FromForm] string? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                           ?? User.FindFirst("sub");

        if (ownerIdClaim == null)
            return Unauthorized(new { error = "Invalid token: user ID not found." });

        if (!Guid.TryParse(ownerIdClaim.Value, out var ownerId))
            return BadRequest(new { error = "Invalid user ID format." });

        if (string.IsNullOrWhiteSpace(prompt))
            return BadRequest(new { error = "Prompt is required." });

        ProcessingParameters? processingParams = null;
        if (!string.IsNullOrWhiteSpace(parameters))
        {
            try
            {
                processingParams = JsonSerializer.Deserialize<ProcessingParameters>(parameters);
            }
            catch (JsonException)
            {
                return BadRequest(new { error = "Invalid parameters JSON format." });
            }
        }

        // Determine feature mode
        ProcessingFeature? processingFeature = feature.HasValue
            ? (ProcessingFeature)feature.Value
            : null;

        // Auto-detect feature if not specified
        if (!processingFeature.HasValue)
        {
            processingFeature = file != null ? ProcessingFeature.Editor : ProcessingFeature.Generator;
        }

        // Validate based on feature mode
        if (processingFeature.Value == ProcessingFeature.Editor)
        {
            // Editor mode (image-to-image) requires file and model parameter
            if (file == null)
                return BadRequest(new { error = "Editor mode (Feature=1) requires an image file for image-to-image editing." });
            
            if (processingParams == null || string.IsNullOrWhiteSpace(processingParams.Model))
                return BadRequest(new { error = "Editor mode (Feature=1) requires 'model' in parameters. Valid models: gpt-image-1-mini-low, gpt-image-1-mini-high, kontext." });
        }
        // Generator mode (text-to-image) with Feature=0: file is optional

        var request = new UploadImageRequest(file, ownerId, prompt, processingFeature, processingParams);
        var result = await _imageService.UploadAsync(request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }
}
