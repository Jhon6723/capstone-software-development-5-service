using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace API.Models;

/// <summary>
/// Request model for creating a project
/// </summary>
public class CreateProjectRequest
{
    /// <summary>
    /// Project name
    /// </summary>
    [Required]
    [DefaultValue("My Awesome Project")]
    public required string Name { get; set; }

    /// <summary>
    /// Project description
    /// </summary>
    [DefaultValue("This is a test project for image processing")]
    public string? Description { get; set; }

    /// <summary>
    /// List of team member user IDs
    /// </summary>
    public List<string>? TeamMemberIds { get; set; }
}

/// <summary>
/// Request model for updating a project
/// </summary>
public class UpdateProjectRequest
{
    [DefaultValue("Updated Project Name")]
    public string? Name { get; set; }

    /// <summary>
    /// Project description
    /// </summary>
    [DefaultValue("Updated project description")]
    /// <summary>
    /// Project description
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Project information
/// </summary>
public class ProjectResponse
{
    /// <summary>
    /// Project unique identifier
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Project name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Project description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Project owner user ID
    /// </summary>
    public required string OwnerId { get; set; }

    /// <summary>
    /// List of team members
    /// </summary>
    public List<string>? TeamMemberIds { get; set; }

    /// <summary>
    /// Project creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update date
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Request model for adding a member to a project
/// </summary>
public class AddMemberRequest
{
    /// <summary>
    [Required]
    [DefaultValue("a6f07985-f8de-48fb-911b-6bde2504f31e")]
    /// User ID to add to the project
    /// </summary>
    public required string UserId { get; set; }
}

/// <summary>
/// Paginated list of projects
/// </summary>
public class ProjectListResponse
{
    /// <summary>
    /// List of projects
    /// </summary>
    public required List<ProjectResponse> Projects { get; set; }

    /// <summary>
    /// Total number of projects
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }
}
