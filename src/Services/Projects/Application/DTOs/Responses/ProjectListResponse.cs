namespace PixPro.Services.Projects.Application.DTOs.Responses;

public class ProjectListResponse
{
    public required List<ProjectResponse> Data { get; set; }
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
}
