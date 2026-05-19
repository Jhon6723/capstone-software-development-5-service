using FluentAssertions;
using MediatR;
using Moq;
using PixPro.Services.Notifications.Application.Commands.CreateNotification;
using PixPro.Services.Notifications.Application.Common.Results;
using PixPro.Services.Notifications.Application.DTOs.Responses;
using PixPro.Services.Notifications.Application.Services;
using PixPro.Services.Notifications.Domain.Entities;
using PixPro.Services.Notifications.Domain.Enums;
using PixPro.Services.Notifications.Domain.Events;
using PixPro.Services.Notifications.Domain.Repositories;

namespace PixPro.UnitTests.Services.Notifications.Commands;

public class CreateNotificationCommandHandlerTests
{
    private readonly Mock<INotificationRepository> _repositoryMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly CreateNotificationCommandHandler _sut;

    public CreateNotificationCommandHandlerTests()
    {
        _repositoryMock = new Mock<INotificationRepository>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _sut = new CreateNotificationCommandHandler(_repositoryMock.Object, _eventPublisherMock.Object);
    }

    #region CreateNotificationCommandHandler Tests

    // TEST 1: Verify that a notification can be created successfully with valid data
    // Ensures that when valid notification data is provided, the notification is created,
    // persisted to the repository, an event is published, and a success result is returned
    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateNotificationAndReturnSuccess()
    {
        // Arrange
        var command = new CreateNotificationCommand(
            UserId: "user123",
            Type: NotificationType.Email,
            Title: "Test Title",
            Message: "Test Message",
            Metadata: new Dictionary<string, string> { { "key", "value" } }
        );

        var createdNotification = new Notification
        {
            Id = "notif123",
            UserId = command.UserId,
            Type = command.Type,
            Title = command.Title,
            Message = command.Message,
            Metadata = command.Metadata,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(x => x.CreateAsync(It.IsAny<Notification>()))
            .ReturnsAsync(createdNotification);

        _eventPublisherMock.Setup(x => x.PublishAsync(It.IsAny<NotificationCreatedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be("notif123");
        result.Value.UserId.Should().Be("user123");
        result.Value.Type.Should().Be(NotificationType.Email);
        result.Value.Title.Should().Be("Test Title");
        result.Value.Message.Should().Be("Test Message");
        
        _repositoryMock.Verify(x => x.CreateAsync(It.Is<Notification>(n => 
            n.UserId == command.UserId && 
            n.Type == command.Type && 
            n.Title == command.Title)), 
            Times.Once);
        
        _eventPublisherMock.Verify(x => x.PublishAsync(It.Is<NotificationCreatedEvent>(e => 
            e.NotificationId == createdNotification.Id && 
            e.UserId == command.UserId), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // TEST 2: Verify that notification creation fails when UserId is empty
    // Tests that an empty UserId returns a failure result with an appropriate error message
    // and no repository or event publisher operations are performed
    [Fact]
    public async Task Handle_EmptyUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateNotificationCommand(
            UserId: "",
            Type: NotificationType.Email,
            Title: "Title",
            Message: "Message",
            Metadata: null
        );

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("UserId is required");
        _repositoryMock.Verify(x => x.CreateAsync(It.IsAny<Notification>()), Times.Never);
        _eventPublisherMock.Verify(x => x.PublishAsync(It.IsAny<NotificationCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // TEST 3: Verify that notification creation fails when UserId contains only whitespace
    // Tests that a whitespace-only UserId is treated as invalid and returns a failure
    [Fact]
    public async Task Handle_WhiteSpaceUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateNotificationCommand(
            UserId: "   ",
            Type: NotificationType.Email,
            Title: "Title",
            Message: "Message",
            Metadata: null
        );

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("UserId is required");
    }

    // TEST 4: Verify that notification creation fails when Title is empty
    // Ensures that Title is a required field and cannot be empty
    [Fact]
    public async Task Handle_EmptyTitle_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateNotificationCommand(
            UserId: "user123",
            Type: NotificationType.Email,
            Title: "",
            Message: "Message",
            Metadata: null
        );

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Title is required");
    }

    // TEST 5: Verify that notification creation fails when Message is empty
    // Ensures that Message is a required field and cannot be empty
    [Fact]
    public async Task Handle_EmptyMessage_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateNotificationCommand(
            UserId: "user123",
            Type: NotificationType.Email,
            Title: "Title",
            Message: "",
            Metadata: null
        );

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Message is required");
    }

    // TEST 6: Verify that notification creation handles repository exceptions gracefully
    // Simulates a database error during creation and expects a failure result
    // with the error message containing the exception details
    [Fact]
    public async Task Handle_RepositoryThrowsException_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateNotificationCommand(
            UserId: "user123",
            Type: NotificationType.Email,
            Title: "Title",
            Message: "Message",
            Metadata: null
        );

        _repositoryMock.Setup(x => x.CreateAsync(It.IsAny<Notification>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Error creating notification");
        result.Error.Should().Contain("Database error");
    }

    // TEST 7: Verify that metadata is preserved when creating a notification
    // Ensures that any custom metadata provided with the notification is correctly
    // stored and can be retrieved in the response
    [Fact]
    public async Task Handle_WithMetadata_ShouldPreserveMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            { "source", "test" },
            { "priority", "high" }
        };

        var command = new CreateNotificationCommand(
            UserId: "user123",
            Type: NotificationType.Push,
            Title: "Push Title",
            Message: "Push Message",
            Metadata: metadata
        );

        var createdNotification = new Notification
        {
            Id = "notif123",
            UserId = command.UserId,
            Type = command.Type,
            Title = command.Title,
            Message = command.Message,
            Metadata = metadata,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(x => x.CreateAsync(It.IsAny<Notification>()))
            .ReturnsAsync(createdNotification);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Metadata.Should().NotBeNull();
        result.Value.Metadata.Should().ContainKey("source");
        result.Value.Metadata!["source"].Should().Be("test");
    }

    #endregion
}
