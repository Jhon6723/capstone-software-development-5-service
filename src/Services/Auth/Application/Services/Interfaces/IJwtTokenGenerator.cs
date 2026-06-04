namespace PixPro.Services.Auth.Application.Services.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(Guid userId, string email, string role);
}
