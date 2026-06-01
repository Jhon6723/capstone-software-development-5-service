namespace PixPro.Services.Projects.Application.DTOs.Requests;

public class CreateProjectRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
}
