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
    /// Upload an image or generate from text (AI-powered image processing)
    /// </summary>
    /// <remarks>
    /// Supports both image-to-image editing and text-to-image generation.
    ///
    /// <b>Processing Modes:</b>
    /// <ul>
    ///   <li><b>Feature=0 (Generator)</b>: Text-to-image generation. File is optional. Uses <c>flux-schnell</c> automatically.</li>
    ///   <li><b>Feature=1 (Editor)</b>: Image-to-image editing. Requires image file AND model parameter.</li>
    /// </ul>
    ///
    /// <b>Validation Rules:</b>
    /// <ul>
    ///   <li><b>Prompt</b>: Required in all modes</li>
    ///   <li><b>Feature=0 (Generator)</b>: No file needed. Parameters optional.</li>
    ///   <li><b>Feature=1 (Editor)</b>: Requires File + Parameters.model</li>
    /// </ul>
    ///
    /// <b>Auto-detection (if Feature not specified):</b>
    /// <ul>
    ///   <li>With file → Editor (Feature=1)</li>
    ///   <li>Without file → Generator (Feature=0)</li>
    /// </ul>
    ///
    /// <b>Processing Models (Editor mode only):</b>
    /// - <c>gpt-image-1-mini-low</c> - Image editing (Default tier, ~$0.011/img)
    /// - <c>kontext</c> - High-quality editing (Standard tier, ~$0.04/img)
    /// - <c>gpt-image-1-mini-high</c> - Premium editing (AI Reasoning tier, ~$0.167/img)
    ///
    /// <b>Example - Text-to-Image:</b>
    /// <code>
    /// {
    ///   "prompt": "A futuristic car in neon city",
    ///   "feature": 0
    /// }
    /// </code>
    ///
    /// <b>Example - Image-to-Image:</b>
    /// <code>
    /// {
    ///   "file": [upload image],
    ///   "prompt": "Add a moon in the sky",
    ///   "feature": 1,
    ///   "parameters": {"model": "gpt-image-1-mini-low"}
    /// }
    /// </code>
    ///
    /// The image will be processed asynchronously. Status updates are sent via WebSocket.
    /// Requires valid JWT token.
    /// </remarks>
    /// <param name="request">Image upload request with optional file, required prompt, and processing parameters</param>
    /// <returns>Image upload confirmation with ImageId</returns>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ImageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [RequestSizeLimit(10485760)] // 10 MB
    public IActionResult UploadImage([FromForm] UploadImageRequest request)
    {
        throw new NotImplementedException("This endpoint is proxied by YARP to Projects service");
    }

}
