namespace PixPro.Services.Auth.Application.DTOs.Responses;

public sealed class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public UserResponse User { get; set; } = null!;
}
