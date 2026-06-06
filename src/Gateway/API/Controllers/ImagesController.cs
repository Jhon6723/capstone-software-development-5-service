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
    /// <b>Processing Modes (Feature is required):</b>
    /// <ul>
    ///   <li><b>Feature=0 (Generator)</b>: Text-to-image generation. File is optional. Uses <c>flux-schnell</c> automatically.</li>
    ///   <li><b>Feature=1 (Editor)</b>: Image-to-image editing. Requires image file AND model parameter.</li>
    /// </ul>
    ///
    /// <b>Validation Rules:</b>
    /// <ul>
    ///   <li><b>ProjectId</b>: Required. Must be a valid project GUID</li>
    ///   <li><b>Feature</b>: Required. Must be 0 (Generator) or 1 (Editor)</li>
    ///   <li><b>Prompt</b>: Required in all modes</li>
    ///   <li><b>Feature=0 (Generator)</b>: No file needed. Parameters optional.</li>
    ///   <li><b>Feature=1 (Editor)</b>: Requires File + Parameters.model</li>
    /// </ul>
    ///
    /// <b>Processing Models (Editor mode only):</b>
    /// - <c>gpt-image-1-mini-low</c> - Image editing (Default tier, ~$0.011/img) — costs 1 credit
    /// - <c>kontext</c> - High-quality editing (Standard tier, ~$0.04/img) — costs 1 credit
    /// - <c>gpt-image-1-mini-high</c> - Premium editing (AI Reasoning tier, ~$0.167/img) — costs 1 credit
    ///
    /// <b>Credit System:</b>
    /// Editor mode (Feature=1) consumes 1 credit per model tier. Free tier allocation:
    /// <c>gpt-image-1-mini-low</c>=5, <c>kontext</c>=3, <c>gpt-image-1-mini-high</c>=1.
    /// Generator mode (Feature=0) is always free — no credits consumed.
    /// Credits are refunded automatically on processing failure (except content moderation violations).
    /// Use <c>GET /api/credits/me</c> to check your balance.
    ///
    /// <b>Example - Text-to-Image:</b>
    /// <code>
    /// {
    ///   "projectId": "550e8400-e29b-41d4-a716-446655440000",
    ///   "prompt": "A futuristic car in neon city",
    ///   "feature": 0
    /// }
    /// </code>
    ///
    /// <b>Example - Image-to-Image:</b>
    /// <code>
    /// {
    ///   "projectId": "550e8400-e29b-41d4-a716-446655440000",
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
    /// <param name="request">Image upload request with optional file, required projectId, prompt and feature, and optional processing parameters</param>
    /// <returns>Image upload confirmation with ImageId</returns>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ImageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    [RequestSizeLimit(10485760)] // 10 MB
    public IActionResult UploadImage([FromForm] UploadImageRequest request)
    {
        throw new NotImplementedException("This endpoint is proxied by YARP to Projects service");
    }

    /// <summary>
    /// Delete an image by ID
    /// </summary>
    /// <remarks>
    /// Permanently deletes the image record from the database and removes the asset from Cloudinary.
    ///
    /// <b>Authorization rules:</b>
    /// <ul>
    ///   <li>Regular users can only delete images they own.</li>
    ///   <li>Admin users can delete any image.</li>
    /// </ul>
    ///
    /// Returns <c>204 No Content</c> on success.
    /// </remarks>
    /// <param name="id">Image GUID to delete</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteImage(string id)
    {
        throw new NotImplementedException("This endpoint is proxied by YARP to Projects service");
    }

}
