using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API.Models;

namespace API.Controllers;

/// <summary>
/// Project management endpoints (proxied to Projects service)
/// </summary>
[Authorize]
[ApiController]
[Route("api/projects")]
[Produces("application/json")]
public class ProjectsController : ControllerBase
{
    /// <summary>
    /// Get paginated list of projects for the authenticated user
    /// </summary>
    /// <remarks>
    /// Returns a paginated list of projects with metadata.
    /// 
    /// <b>Query Parameters:</b>
    /// <ul>
    ///   <li><b>page</b>: Page number (default: 1)</li>
    ///   <li><b>limit</b>: Items per page, max 100 (default: 12)</li>
    ///   <li><b>search</b>: Optional search filter by project name</li>
    /// </ul>
    /// 
    /// <b>Response:</b>
    /// <ul>
    ///   <li><b>data</b>: List of projects</li>
    ///   <li><b>total</b>: Total number of projects</li>
    ///   <li><b>page</b>: Current page number</li>
    ///   <li><b>pageSize</b>: Items per page</li>
    ///   <li><b>hasMore</b>: Indicates if there are more pages</li>
    /// </ul>
    /// 
    /// Requires valid JWT token.
    /// </remarks>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="limit">Items per page (default: 12)</param>
    /// <param name="search">Optional search term</param>
    /// <returns>Paginated list of projects</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ProjectListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetList(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 12,
        [FromQuery] string? search = null)
    {
        throw new NotImplementedException("This endpoint is proxied by YARP to Projects service");
    }

    /// <summary>
    /// Get a single project by ID
    /// </summary>
    /// <remarks>
    /// Returns detailed information about a specific project.
    /// Requires valid JWT token.
    /// </remarks>
    /// <param name="id">Project ID</param>
    /// <returns>Project details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(string id)
    {
        throw new NotImplementedException("This endpoint is proxied by YARP to Projects service");
    }

    /// <summary>
    /// Create a new project
    /// </summary>
    /// <remarks>
    /// Creates a new project for the authenticated user.
    /// Requires valid JWT token.
    /// </remarks>
    /// <param name="request">Project creation request</param>
    /// <returns>Created project details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Create([FromBody] CreateProjectRequest request)
    {
        throw new NotImplementedException("This endpoint is proxied by YARP to Projects service");
    }

    /// <summary>
    /// Update an existing project
    /// </summary>
    /// <remarks>
    /// Updates project information.
    /// Requires valid JWT token and project ownership.
    /// </remarks>
    /// <param name="id">Project ID</param>
    /// <param name="request">Project update request</param>
    /// <returns>Updated project details</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Update(string id, [FromBody] UpdateProjectRequest request)
    {
        throw new NotImplementedException("This endpoint is proxied by YARP to Projects service");
    }

    /// <summary>
    /// Delete a project
    /// </summary>
    /// <remarks>
    /// Permanently deletes a project and all its images.
    /// Requires valid JWT token and project ownership.
    /// </remarks>
    /// <param name="id">Project ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(string id)
    {
        throw new NotImplementedException("This endpoint is proxied by YARP to Projects service");
    }

    /// <summary>
    /// Add a member to a project
    /// </summary>
    /// <remarks>
    /// Adds a team member to the project.
    /// Requires valid JWT token and project ownership.
    /// </remarks>
    /// <param name="id">Project ID</param>
    /// <param name="request">Add member request</param>
    /// <returns>Updated project details</returns>
    [HttpPost("{id}/members")]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult AddMember(string id, [FromBody] AddMemberRequest request)
    {
        throw new NotImplementedException("This endpoint is proxied by YARP to Projects service");
    }
}
