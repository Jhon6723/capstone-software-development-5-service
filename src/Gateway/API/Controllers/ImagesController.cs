using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API.Models;

namespace API.Controllers;

/// <summary>
/// Image management endpoints (proxied to Projects service)
/// </summary>
[Authorize]
[ApiController]
[Route("api/images")]
[Produces("application/json")]
public class ImagesController : ControllerBase
{
    /// <summary>
    /// Upload a new image to a project
    /// </summary>
    /// <remarks>
    /// Uploads an image file (max 10MB). Accepts multipart/form-data.
    /// The image will be processed asynchronously.
    /// Requires valid JWT token.
    /// </remarks>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [RequestSizeLimit(10485760)] // 10 MB
    public IActionResult UploadImage([FromForm] UploadImageRequest request)
    {
        throw new NotImplementedException("This endpoint is proxied by YARP");
    }

}
