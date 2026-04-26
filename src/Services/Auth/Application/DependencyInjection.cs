using Microsoft.Extensions.DependencyInjection;
using PixPro.Services.Auth.Application.Services.Implementations;
using PixPro.Services.Auth.Application.Services.Interfaces;

namespace PixPro.Services.Auth.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register Application Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
