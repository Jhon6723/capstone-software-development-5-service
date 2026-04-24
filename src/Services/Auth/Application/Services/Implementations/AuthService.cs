using PixPro.Services.Auth.Application.Common.Results;
using PixPro.Services.Auth.Application.DTOs.Requests;
using PixPro.Services.Auth.Application.DTOs.Responses;
using PixPro.Services.Auth.Application.Services.Interfaces;
using PixPro.Services.Auth.Domain.Entities;
using PixPro.Services.Auth.Domain.Repositories;
using PixPro.Services.Auth.Domain.Specifications;

namespace PixPro.Services.Auth.Application.Services.Implementations;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;

    public AuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserResponse>> RegisterUserAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate email uniqueness using Specification
            var uniqueEmailSpec = new UniqueEmailSpecification(_userRepository);
            var isEmailUnique = await uniqueEmailSpec.IsSatisfiedByAsync(request.Email, cancellationToken);

            if (!isEmailUnique)
            {
                return Error.Conflict(
                    "User.EmailAlreadyExists",
                    $"A user with email '{request.Email}' already exists.");
            }

            // Create User entity
            var user = new User(request.Auth0Id, request.Email);

            // Save to repository
            await _userRepository.AddAsync(user, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);

            // Map to response
            var response = new UserResponse
            {
                Id = user.Id,
                Auth0Id = user.Auth0Id,
                Email = user.Email,
                CreatedAt = user.CreatedAt
            };

            return Result<UserResponse>.Success(response);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(
                "User.InvalidData",
                ex.Message);
        }
        catch (Exception ex)
        {
            return Error.Failure(
                "User.RegistrationFailed",
                $"An error occurred while registering the user: {ex.Message}");
        }
    }

    public async Task<Result<UserResponse>> GetUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

            if (user is null)
            {
                return Error.NotFound(
                    "User.NotFound",
                    $"User with email '{email}' was not found.");
            }

            var response = new UserResponse
            {
                Id = user.Id,
                Auth0Id = user.Auth0Id,
                Email = user.Email,
                CreatedAt = user.CreatedAt
            };

            return Result<UserResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Error.Failure(
                "User.RetrievalFailed",
                $"An error occurred while retrieving the user: {ex.Message}");
        }
    }
}
