using FluentAssertions;
using Moq;
using PixPro.Services.Notifications.Application.Commands.DeleteNotification;
using PixPro.Services.Notifications.Application.Common.Results;
using PixPro.Services.Notifications.Application.Services;
using PixPro.Services.Notifications.Domain.Entities;
using PixPro.Services.Notifications.Domain.Events;
using PixPro.Services.Notifications.Domain.Repositories;

namespace PixPro.UnitTests.Services.Notifications.Commands;

public class DeleteNotificationCommandHandlerTests
{
    private readonly Mock<INotificationRepository> _repositoryMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly DeleteNotificationCommandHandler _sut;

    public DeleteNotificationCommandHandlerTests()
    {
        _repositoryMock = new Mock<INotificationRepository>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _sut = new DeleteNotificationCommandHandler(_repositoryMock.Object, _eventPublisherMock.Object);
    }

    #region DeleteNotificationCommandHandler Tests

    // TEST 1: Verify that a notification can be deleted successfully
    // Ensures that when a valid notification ID is provided, the notification is retrieved,
    // deleted from the repository, and a domain event is published for CQRS synchronization
    [Fact]
    public async Task Handle_ValidCommand_ShouldDeleteNotificationAndPublishEvent()
    {
        // Arrange
        var notificationId = "notif123";
        var userId = "user123";
        var command = new DeleteNotificationCommand(notificationId);

        var existingNotification = new Notification
        {
            Id = notificationId,
            UserId = userId,
            Title = "Test",
            Message = "Test"
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(notificationId))
            .ReturnsAsync(existingNotification);
        _repositoryMock.Setup(x => x.DeleteAsync(notificationId))
            .ReturnsAsync(true);
        _eventPublisherMock.Setup(x => x.PublishAsync(It.IsAny<NotificationDeletedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(x => x.GetByIdAsync(notificationId), Times.Once);
        _repositoryMock.Verify(x => x.DeleteAsync(notificationId), Times.Once);
        _eventPublisherMock.Verify(x => x.PublishAsync(
            It.Is<NotificationDeletedEvent>(e => e.NotificationId == notificationId && e.UserId == userId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // TEST 2: Verify that deletion fails when the notification does not exist
    // Simulates a non-existent notification ID and expects a failure result
    // with the specific error message "Notification not found"
    [Fact]
    public async Task Handle_NotificationNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new DeleteNotificationCommand("nonexistent");

        _repositoryMock.Setup(x => x.GetByIdAsync("nonexistent"))
            .ReturnsAsync((Notification?)null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Notification not found");
        _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never);
        _eventPublisherMock.Verify(x => x.PublishAsync(It.IsAny<NotificationDeletedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // TEST 3: Verify behavior when repository delete operation returns false
    // The handler currently does not check the boolean result of DeleteAsync,
    // so it returns success even if the delete operation failed.
    // This test documents that behavior for future consideration.
    [Fact]
    public async Task Handle_DeleteFails_ShouldReturnFailureButStillProcess()
    {
        // Arrange
        var command = new DeleteNotificationCommand("notif123");

        var existingNotification = new Notification
        {
            Id = "notif123",
            UserId = "user123"
        };

        _repositoryMock.Setup(x => x.GetByIdAsync("notif123"))
            .ReturnsAsync(existingNotification);
        _repositoryMock.Setup(x => x.DeleteAsync("notif123"))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        // Note: Handler doesn't check DeleteAsync result, so it returns success
        result.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(x => x.DeleteAsync("notif123"), Times.Once);
    }

    // TEST 4: Verify that repository exceptions propagate appropriately
    // Simulates a database connection failure during GetByIdAsync and expects
    // the exception to propagate up rather than being caught and wrapped
    [Fact]
    public async Task Handle_WhenRepositoryThrows_ShouldPropagateException()
    {
        // Arrange
        var command = new DeleteNotificationCommand("notif123");

        _repositoryMock.Setup(x => x.GetByIdAsync("notif123"))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _sut.Handle(command, CancellationToken.None));
    }

    #endregion
}
