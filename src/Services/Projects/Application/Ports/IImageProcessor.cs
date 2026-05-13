namespace PixPro.Services.Projects.Application.Ports;

public interface IImageProcessor
{
    Task<string> ProcessImageAsync(string imagePath, CancellationToken cancellationToken);
}
