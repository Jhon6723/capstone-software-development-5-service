using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using PixPro.Services.Projects.Application.Common.Results;
using PixPro.Services.Projects.Application.DTOs.Requests;
using PixPro.Services.Projects.Application.DTOs.Responses;
using PixPro.Services.Projects.Application.IntegrationEvents;
using PixPro.Services.Projects.Application.Interfaces;
using PixPro.Services.Projects.Application.Services;
using PixPro.Services.Projects.Domain.Entities;
using PixPro.Services.Projects.Domain.Repositories;

namespace PixPro.UnitTests.Services.Projects.Services;

public class ImageServiceTests
{
    private readonly Mock<IImageRepository> _imageRepositoryMock;
    private readonly Mock<IStorageService> _storageServiceMock;
    private readonly Mock<ILogger<ImageService>> _loggerMock;
    private readonly Mock<IMessagePublisher> _messagePublisherMock;
    private readonly ImageService _sut;

    public ImageServiceTests()
    {
        _imageRepositoryMock = new Mock<IImageRepository>();
        _storageServiceMock = new Mock<IStorageService>();
        _loggerMock = new Mock<ILogger<ImageService>>();
        _messagePublisherMock = new Mock<IMessagePublisher>();
        _sut = new ImageService(
            _imageRepositoryMock.Object,
            _storageServiceMock.Object,
            _loggerMock.Object,
            _messagePublisherMock.Object);
    }

    private Mock<IFormFile> CreateMockImageFile(string fileName = "test.jpg", string contentType = "image/jpeg", long length = 1024 * 1024)
    {
        var fileMock = new Mock<IFormFile>();
        var stream = new MemoryStream();
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.ContentType).Returns(contentType);
        fileMock.Setup(f => f.Length).Returns(length);
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
        return fileMock;
    }

    #region ImageService UploadAsync Tests

    // TEST 1: Verify that a valid image can be uploaded successfully
    // Ensures that when a valid image file is provided, it is uploaded to Cloudinary,
    // saved to the repository, and an ImageUploadedEvent is published to the message broker
    [Fact]
    public async Task UploadAsync_ValidImage_ShouldUploadAndReturnSuccess()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var fileMock = CreateMockImageFile();
        var request = new UploadImageRequest(fileMock.Object, projectId, ownerId, "test.jpg", null, null);

        var uploadResult = new StorageUploadResult(
            PublicId: "pub123",
            Url: "http://example.com/image.jpg",
            SecureUrl: "https://example.com/image.jpg",
            Bytes: 1024,
            Format: "jpg",
            Width: 800,
            Height: 600
        );

        _storageServiceMock.Setup(x => x.UploadAsync(
                It.IsAny<Stream>(),
                fileMock.Object.FileName,
                fileMock.Object.ContentType,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResult);

        _imageRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Image>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _imageRepositoryMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _messagePublisherMock.Setup(x => x.PublishAsync(
                It.IsAny<ImageUploadedEvent>(),
                "image-events",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.UploadAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.FileName.Should().Be("test.jpg");
        result.Value.SecureUrl.Should().Be("https://example.com/image.jpg");
        
        _storageServiceMock.Verify(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _imageRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Image>(), It.IsAny<CancellationToken>()), Times.Once);
        _imageRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _messagePublisherMock.Verify(x => x.PublishAsync(It.IsAny<ImageUploadedEvent>(), "image-events", It.IsAny<CancellationToken>()), Times.Once);
    }

    // TEST 2: Verify that upload handles null file gracefully
    // Tests that a null file is processed successfully by the service
    [Fact]
    public async Task UploadAsync_NullFile_ShouldReturnSuccess()
    {
        // Arrange
        var request = new UploadImageRequest(null!, Guid.NewGuid(), Guid.NewGuid(), "test.jpg", null, null);

        // Act
        var result = await _sut.UploadAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    // TEST 3: Verify that upload handles empty file gracefully
    // Tests that an empty file is processed successfully by the service
    [Fact]
    public async Task UploadAsync_EmptyFile_ShouldReturnSuccess()
    {
        // Arrange
        var fileMock = CreateMockImageFile(length: 0);
        var request = new UploadImageRequest(fileMock.Object, Guid.NewGuid(), Guid.NewGuid(), "test.jpg", null, null);

        // Act
        var result = await _sut.UploadAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    // TEST 4: Verify that upload fails when the file exceeds the 5 MB size limit
    // Tests that a file larger than 5 MB returns a failure with the appropriate error message
    [Fact]
    public async Task UploadAsync_FileTooLarge_ShouldReturnFailure()
    {
        // Arrange
        var fileMock = CreateMockImageFile(length: 6 * 1024 * 1024); // 6 MB
        var request = new UploadImageRequest(fileMock.Object, Guid.NewGuid(), Guid.NewGuid(), "test.jpg", null, null);

        // Act
        var result = await _sut.UploadAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("File exceeds the 5 MB size limit.");
    }

    // TESTS 5-8: Verify that upload fails when the file has an invalid extension
    // Tests 4 different invalid file extensions:
    //   - Test 5: .txt (text file)
    //   - Test 6: .gif (GIF image - not allowed)
    //   - Test 7: .bmp (BMP image - not allowed)
    //   - Test 8: .pdf (PDF document)
    // Each test expects a failure result indicating the extension is not allowed
    [Theory]
    [InlineData("test.txt")]
    [InlineData("test.gif")]
    [InlineData("test.bmp")]
    [InlineData("test.pdf")]
    public async Task UploadAsync_InvalidExtension_ShouldReturnFailure(string fileName)
    {
        // Arrange
        var fileMock = CreateMockImageFile(fileName: fileName);
        var request = new UploadImageRequest(fileMock.Object, Guid.NewGuid(), Guid.NewGuid(), fileName, null, null);

        // Act
        var result = await _sut.UploadAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Extension");
        result.Error.Should().Contain("not allowed");
    }

    // TESTS 9-11: Verify that upload fails when the file has an invalid content type
    // Tests 3 different invalid content types:
    //   - Test 9: image/bmp (BMP image - not allowed)
    //   - Test 10: image/gif (GIF image - not allowed)
    //   - Test 11: application/pdf (PDF document)
    // Each test expects a failure result indicating the content type is not allowed
    [Theory]
    [InlineData("image/bmp")]
    [InlineData("image/gif")]
    [InlineData("application/pdf")]
    public async Task UploadAsync_InvalidContentType_ShouldReturnFailure(string contentType)
    {
        // Arrange
        var fileMock = CreateMockImageFile(contentType: contentType);
        var request = new UploadImageRequest(fileMock.Object, Guid.NewGuid(), Guid.NewGuid(), "test.jpg", null, null);

        // Act
        var result = await _sut.UploadAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Content-Type");
        result.Error.Should().Contain("not allowed");
    }

    // TEST 12: Verify that exceptions from the storage service are handled gracefully
    // Simulates a Cloudinary error during upload and expects a failure result
    // with the error message containing the exception details
    [Fact]
    public async Task UploadAsync_StorageServiceThrowsException_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var fileMock = CreateMockImageFile();
        var request = new UploadImageRequest(fileMock.Object, projectId, ownerId, "test.jpg", null, null);

        _storageServiceMock.Setup(x => x.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Cloudinary error"));

        // Act
        var result = await _sut.UploadAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Error processing image request");
        result.Error.Should().Contain("Cloudinary error");
    }

    // TEST 13: Verify that image upload succeeds even if message publishing fails
    // Ensures that the image upload operation is resilient - even if RabbitMQ is unavailable,
    // the image is still saved successfully. Verifies that a warning is logged.
    [Fact]
    public async Task UploadAsync_MessagePublisherFails_ShouldStillReturnSuccess()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var fileMock = CreateMockImageFile();
        var request = new UploadImageRequest(fileMock.Object, projectId, ownerId, "test.jpg", null, null);
        var uploadResult = new StorageUploadResult(
            "pub123",
            "http://example.com/image.jpg",
            "https://example.com/image.jpg",
            1024,
            "jpg",
            800,
            600
        );

        _storageServiceMock.Setup(x => x.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResult);

        _imageRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Image>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _imageRepositoryMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _messagePublisherMock.Setup(x => x.PublishAsync(
                It.IsAny<ImageUploadedEvent>(),
                "image-events",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("RabbitMQ unavailable"));

        // Act
        var result = await _sut.UploadAsync(request, CancellationToken.None);

        // Assert - Image upload still succeeds even if message publishing fails
        result.IsSuccess.Should().BeTrue();
        _messagePublisherMock.Verify(x => x.PublishAsync(It.IsAny<ImageUploadedEvent>(), "image-events", It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify warning was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to publish ImageUploadedEvent")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // TEST 14: Verify that valid PNG images can be uploaded successfully
    // Tests that PNG files (allowed format) are accepted and processed correctly
    [Fact]
    public async Task UploadAsync_ValidPngImage_ShouldUploadSuccessfully()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var fileMock = CreateMockImageFile(fileName: "image.png", contentType: "image/png");
        var request = new UploadImageRequest(fileMock.Object, projectId, ownerId, "image.png", null, null);
        var uploadResult = new StorageUploadResult(
            "pub456",
            "http://example.com/image.png",
            "https://example.com/image.png",
            2048,
            "png",
            1920,
            1080
        );

        _storageServiceMock.Setup(x => x.UploadAsync(
                It.IsAny<Stream>(),
                fileMock.Object.FileName,
                fileMock.Object.ContentType,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResult);

        _imageRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Image>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _imageRepositoryMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _messagePublisherMock.Setup(x => x.PublishAsync(
                It.IsAny<ImageUploadedEvent>(),
                "image-events",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.UploadAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Format.Should().Be("png");
        result.Value.FileName.Should().Be("image.png");
    }

    // TEST 15: Verify that valid WebP images can be uploaded successfully
    // Tests that WebP files (allowed format) are accepted and processed correctly
    [Fact]
    public async Task UploadAsync_ValidWebpImage_ShouldUploadSuccessfully()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var fileMock = CreateMockImageFile(fileName: "image.webp", contentType: "image/webp");
        var request = new UploadImageRequest(fileMock.Object, projectId, ownerId, "image.webp", null, null);

        var uploadResult = new StorageUploadResult(
            "pub789",
            "http://example.com/image.webp",
            "https://example.com/image.webp",
            512,
            "webp",
            400,
            300
        );

        _storageServiceMock.Setup(x => x.UploadAsync(
                It.IsAny<Stream>(),
                fileMock.Object.FileName,
                fileMock.Object.ContentType,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResult);

        _imageRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Image>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _imageRepositoryMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _messagePublisherMock.Setup(x => x.PublishAsync(
                It.IsAny<ImageUploadedEvent>(),
                "image-events",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.UploadAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Format.Should().Be("webp");
    }

    #endregion
}
