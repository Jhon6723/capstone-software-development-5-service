using PixPro.Services.Auth.Application.Common.Results;
using PixPro.Services.Auth.Application.DTOs.Requests;
using PixPro.Services.Auth.Application.DTOs.Responses;

namespace PixPro.Services.Auth.Application.Services.Interfaces;

public interface IAuthService
{
    Task<Result<UserResponse>> RegisterUserAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<UserResponse>> GetUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<Result<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}
