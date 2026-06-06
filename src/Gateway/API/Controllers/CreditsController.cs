using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API.Models;

namespace API.Controllers;

/// <summary>
/// Credit management endpoints (proxied to Projects service)
/// </summary>
[Authorize]
[ApiController]
[Route("api/credits")]
[Produces("application/json")]
public class CreditsController : ControllerBase
{
    /// <summary>
    /// Get current credit balance for the authenticated user
    /// </summary>
    /// <remarks>
    /// Returns the credit balance for each AI model tier.
    ///
    /// <b>Credit Allocation (Free Tier):</b>
    /// | Model | Credits |
    /// |-------|---------|
    /// | gpt-image-1-mini-low | 5 |
    /// | kontext | 3 |
    /// | gpt-image-1-mini-high | 1 |
    ///
    /// <b>Notes:</b>
    /// <ul>
    ///   <li>Credits are seeded automatically on the first image upload per model tier.</li>
    ///   <li>If no credits are listed for a tier, they haven't been initialized yet (will be seeded on first use).</li>
    ///   <li><c>flux-schnell</c> (Generator mode, Feature=0) is always free — no credits consumed.</li>
    ///   <li>Admin users bypass credit checks entirely.</li>
    /// </ul>
    ///
    /// <b>When credits run out:</b>
    /// Uploading with that model returns <c>402 Payment Required</c> with <c>INSUFFICIENT_CREDITS</c>.
    ///
    /// <b>Credit Refund Policy:</b>
    /// Credits are refunded automatically if the IA Service fails to process the image,
    /// <i>except</i> for <c>CONTENT_MODERATION_VIOLATION</c> errors (intentional abuse deterrent).
    ///
    /// <b>Example Response:</b>
    /// <code>
    /// {
    ///   "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///   "credits": [
    ///     {
    ///       "modelTier": "GptMiniLow",
    ///       "creditsRemaining": 4,
    ///       "creditsTotal": 5,
    ///       "subscriptionTier": "Free",
    ///       "resetAt": null
    ///     },
    ///     {
    ///       "modelTier": "Kontext",
    ///       "creditsRemaining": 3,
    ///       "creditsTotal": 3,
    ///       "subscriptionTier": "Free",
    ///       "resetAt": null
    ///     },
    ///     {
    ///       "modelTier": "GptMiniHigh",
    ///       "creditsRemaining": 1,
    ///       "creditsTotal": 1,
    ///       "subscriptionTier": "Free",
    ///       "resetAt": null
    ///     }
    ///   ]
    /// }
    /// </code>
    /// </remarks>
    /// <returns>Credit balance grouped by model tier</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(CreditBalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetMyCredits()
    {
        throw new NotImplementedException("This endpoint is proxied by YARP to Projects service");
    }
}
