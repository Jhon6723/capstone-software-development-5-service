namespace PixPro.Services.Projects.Application.DTOs.Responses;

public class ProjectResponse
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public int ImageCount { get; set; }
}
