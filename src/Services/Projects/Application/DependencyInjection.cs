using Microsoft.Extensions.DependencyInjection;
using PixPro.Services.Projects.Application.Services;

namespace PixPro.Services.Projects.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IImageService, ImageService>();
        services.AddScoped<IImageProcessingService, ImageProcessingService>();

        return services;
    }
}
