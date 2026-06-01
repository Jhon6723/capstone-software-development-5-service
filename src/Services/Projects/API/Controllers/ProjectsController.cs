using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixPro.Services.Projects.Application.DTOs.Requests;
using PixPro.Services.Projects.Application.Services;
using System.Security.Claims;

namespace PixPro.Services.Projects.API.Controllers;

[ApiController]
[Route("api/projects")]
[Produces("application/json")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetList(
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                           ?? User.FindFirst("sub");

        if (ownerIdClaim == null)
            return Unauthorized(new { error = "Invalid token: user ID not found." });

        if (!Guid.TryParse(ownerIdClaim.Value, out var ownerId))
            return BadRequest(new { error = "Invalid user ID format." });

        var result = await _projectService.GetProjectsAsync(ownerId, search, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken = default)
    {
        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                           ?? User.FindFirst("sub");

        if (ownerIdClaim == null)
            return Unauthorized(new { error = "Invalid token: user ID not found." });

        if (!Guid.TryParse(id, out var projectId))
            return BadRequest(new { error = "Invalid project ID format." });

        var result = await _projectService.GetByIdAsync(projectId, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        if (result.Value == null)
            return NotFound(new { error = "Project not found." });

        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                           ?? User.FindFirst("sub");

        if (ownerIdClaim == null)
            return Unauthorized(new { error = "Invalid token: user ID not found." });

        if (!Guid.TryParse(ownerIdClaim.Value, out var ownerId))
            return BadRequest(new { error = "Invalid user ID format." });

        var result = await _projectService.CreateProjectAsync(ownerId, request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpGet("{id}/images")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProjectImages(
        string id,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 12,
        CancellationToken cancellationToken = default)
    {
        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                           ?? User.FindFirst("sub");

        if (ownerIdClaim == null)
            return Unauthorized(new { error = "Invalid token: user ID not found." });

        if (!Guid.TryParse(id, out var projectId))
            return BadRequest(new { error = "Invalid project ID format." });

        var request = new GetProjectImagesRequest
        {
            Page = page,
            PageSize = limit
        };

        var result = await _projectService.GetProjectImagesAsync(projectId, request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }
}
