using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixPro.Services.Projects.API.Helpers;
using PixPro.Services.Projects.Application.Services;
using System.Security.Claims;

namespace PixPro.Services.Projects.API.Controllers;

[ApiController]
[Route("api/credits")]
[Produces("application/json")]
[Authorize]
public class CreditsController : ControllerBase
{
    private readonly ICreditService _creditService;

    public CreditsController(ICreditService creditService)
    {
        _creditService = creditService;
    }

    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyCredits(CancellationToken cancellationToken)
    {
        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                           ?? User.FindFirst("sub");

        if (ownerIdClaim == null)
            return Unauthorized(new { error = "Invalid token: user ID not found." });

        var userId = UserIdHelper.DeriveGuid(ownerIdClaim.Value);
        var isAdmin = User.IsInRole("Admin");

        var result = await _creditService.GetBalanceAsync(userId, isAdmin, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }
}
