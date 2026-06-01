namespace PixPro.Services.Projects.Application.DTOs.Requests;

public class GetProjectsRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public string? Search { get; set; }
}
