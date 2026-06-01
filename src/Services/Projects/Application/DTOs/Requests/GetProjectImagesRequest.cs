namespace PixPro.Services.Projects.Application.DTOs.Requests;

public class GetProjectImagesRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}
