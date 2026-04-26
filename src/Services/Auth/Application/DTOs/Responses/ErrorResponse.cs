namespace PixPro.Services.Auth.Application.DTOs.Responses;

public sealed record ErrorResponse
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
}
