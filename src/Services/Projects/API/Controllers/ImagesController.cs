using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixPro.Services.Projects.Application.DTOs.Requests;
using PixPro.Services.Projects.Application.Services;
using System.Security.Claims;

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
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                           ?? User.FindFirst("sub");

        if (ownerIdClaim == null)
            return Unauthorized(new { error = "Invalid token: user ID not found." });

        if (!Guid.TryParse(ownerIdClaim.Value, out var ownerId))
            return BadRequest(new { error = "Invalid user ID format." });

        var request = new UploadImageRequest(file, ownerId);
        var result = await _imageService.UploadAsync(request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }
}
