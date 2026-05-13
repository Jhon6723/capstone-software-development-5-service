using Microsoft.Extensions.Logging;
using PixPro.Services.Projects.Application.Ports;

namespace PixPro.Services.Projects.Infrastructure.Processing;

public sealed class MockAiImageProcessor : IImageProcessor
{
    private readonly ILogger<MockAiImageProcessor> _logger;

    public MockAiImageProcessor(ILogger<MockAiImageProcessor> logger)
    {
        _logger = logger;
    }

    public async Task<string> ProcessImageAsync(string imagePath, CancellationToken cancellationToken)
    {
        _logger.LogInformation("MOCK AI  starting image processing for: {ImagePath}", imagePath);

        await Task.Delay(2000, cancellationToken);

        var mockResult = $"mock-ai-result-{Guid.NewGuid():N}";

        _logger.LogInformation("MOCK ai image processed  Result: {Result}", mockResult);

        return mockResult;
    }
}
