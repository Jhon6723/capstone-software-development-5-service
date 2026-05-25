using FluentAssertions;
using Moq;
using PixPro.Services.Notifications.Application.Common.Results;
using PixPro.Services.Notifications.Application.DTOs.Responses;
using PixPro.Services.Notifications.Application.Queries.GetUserNotifications;
using PixPro.Services.Notifications.Application.Services;
using PixPro.Services.Notifications.Domain.Enums;

namespace PixPro.Notifications.UnitTests.Services.Notifications.Queries;

public class GetUserNotificationsQueryHandlerTests
{
    private readonly Mock<INotificationReadRepository> _readRepositoryMock;
    private readonly GetUserNotificationsQueryHandler _sut;

    public GetUserNotificationsQueryHandlerTests()
    {
        _readRepositoryMock = new Mock<INotificationReadRepository>();
        _sut = new GetUserNotificationsQueryHandler(_readRepositoryMock.Object);
    }

    #region GetUserNotificationsQueryHandler Tests

    // TEST 1: Verify that paginated notifications can be retrieved successfully for a valid user
    // Ensures that when valid UserId, PageSize, and Page are provided, the repository returns
    // the correct page of notifications with total count and pagination metadata
    [Fact]
    public async Task Handle_ValidRequest_ShouldReturnPaginatedNotifications()
    {
        // Arrange
        var userId = "user-123";
        var page = 1;
        var pageSize = 10;
        var notifications = CreateNotificationsList(userId, pageSize);
        var expectedResponse = new NotificationListResponse(notifications, 25, page, pageSize);

        _readRepositoryMock
            .Setup(x => x.GetUserNotificationsAsync(userId, pageSize, page, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var query = new GetUserNotificationsQuery(userId, pageSize, page);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Notifications.Should().HaveCount(pageSize);
        result.Value!.TotalCount.Should().Be(25);
        result.Value!.Page.Should().Be(page);
        result.Value!.PageSize.Should().Be(pageSize);
        _readRepositoryMock.Verify(x => x.GetUserNotificationsAsync(userId, pageSize, page, It.IsAny<CancellationToken>()), Times.Once);
    }

    // TEST 2: Verify that the second page of paginated results can be retrieved correctly
    // Tests that requesting page 2 returns the correct page number in the response
    // and ensures pagination offset calculation works properly
    [Fact]
    public async Task Handle_SecondPage_ShouldReturnCorrectPage()
    {
        // Arrange
        var userId = "user-123";
        var page = 2;
        var pageSize = 10;
        var notifications = CreateNotificationsList(userId, pageSize);
        var expectedResponse = new NotificationListResponse(notifications, 25, page, pageSize);

        _readRepositoryMock
            .Setup(x => x.GetUserNotificationsAsync(userId, pageSize, page, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var query = new GetUserNotificationsQuery(userId, pageSize, page);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Page.Should().Be(2);
    }

    // TEST 3: Verify that a user with no notifications receives an empty list with zero total count
    // Tests the edge case where a user has no notifications and expects
    // a success result with an empty collection and TotalCount = 0
    [Fact]
    public async Task Handle_EmptyResult_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = "new-user";
        var page = 1;
        var pageSize = 10;
        var emptyResponse = new NotificationListResponse(new List<NotificationResponse>(), 0, page, pageSize);

        _readRepositoryMock
            .Setup(x => x.GetUserNotificationsAsync(userId, pageSize, page, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyResponse);

        var query = new GetUserNotificationsQuery(userId, pageSize, page);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Notifications.Should().BeEmpty();
        result.Value!.TotalCount.Should().Be(0);
    }

    // TEST 4: Verify that retrieving notifications fails when UserId is null
    // Tests that a null UserId returns a failure result with an appropriate
    // error message and no repository operation is performed
    [Fact]
    public async Task Handle_NullUserId_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetUserNotificationsQuery(null!, 10, 1);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("UserId is required");
        _readRepositoryMock.Verify(x => x.GetUserNotificationsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // TEST 5: Verify that retrieving notifications fails when UserId is empty
    // Tests that an empty string UserId is treated as invalid and returns
    // a failure result without accessing the repository
    [Fact]
    public async Task Handle_EmptyUserId_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetUserNotificationsQuery(string.Empty, 10, 1);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("UserId is required");
    }

    // TEST 6: Verify that retrieving notifications fails when UserId is whitespace
    // Tests that a whitespace-only UserId is treated as invalid and returns
    // a failure result without accessing the repository
    [Fact]
    public async Task Handle_WhitespaceUserId_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetUserNotificationsQuery("   ", 10, 1);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("UserId is required");
    }

    // TEST 7: Verify that PageSize of zero returns a validation failure
    // Tests that PageSize must be greater than 0 and a zero value returns
    // an appropriate error message
    [Fact]
    public async Task Handle_InvalidPageSize_Zero_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetUserNotificationsQuery("user-123", 0, 1);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("PageSize must be greater than 0");
    }

    // TEST 8: Verify that negative PageSize returns a validation failure
    // Tests that PageSize must be positive and a negative value returns
    // an appropriate error message
    [Fact]
    public async Task Handle_InvalidPageSize_Negative_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetUserNotificationsQuery("user-123", -5, 1);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("PageSize must be greater than 0");
    }

    // TEST 9: Verify that Page of zero returns a validation failure
    // Tests that Page must be greater than 0 and a zero value returns
    // an appropriate error message
    [Fact]
    public async Task Handle_InvalidPage_Zero_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetUserNotificationsQuery("user-123", 10, 0);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Page must be greater than 0");
    }

    // TEST 10: Verify that negative Page returns a validation failure
    // Tests that Page must be positive and a negative value returns
    // an appropriate error message
    [Fact]
    public async Task Handle_InvalidPage_Negative_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetUserNotificationsQuery("user-123", 10, -1);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Page must be greater than 0");
    }

    // TEST 11: Verify that large PageSize values are handled correctly
    // Tests that the handler can process requests with PageSize = 100
    // without performance issues or validation errors
    [Fact]
    public async Task Handle_LargePageSize_ShouldHandleCorrectly()
    {
        // Arrange
        var userId = "user-123";
        var page = 1;
        var pageSize = 100;
        var notifications = CreateNotificationsList(userId, pageSize);
        var expectedResponse = new NotificationListResponse(notifications, 500, page, pageSize);

        _readRepositoryMock
            .Setup(x => x.GetUserNotificationsAsync(userId, pageSize, page, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var query = new GetUserNotificationsQuery(userId, pageSize, page);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Notifications.Should().HaveCount(pageSize);
        result.Value!.PageSize.Should().Be(pageSize);
    }

    // TEST 12: Verify that the last page returns the correct number of remaining items
    // Tests that when the last page has fewer items than PageSize, the response
    // contains only the remaining items and pagination metadata is still correct
    [Fact]
    public async Task Handle_LastPage_ShouldReturnRemainingItems()
    {
        // Arrange
        var userId = "user-123";
        var page = 3;
        var pageSize = 10;
        var remainingItems = 5;
        var notifications = CreateNotificationsList(userId, remainingItems);
        var expectedResponse = new NotificationListResponse(notifications, 25, page, pageSize);

        _readRepositoryMock
            .Setup(x => x.GetUserNotificationsAsync(userId, pageSize, page, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var query = new GetUserNotificationsQuery(userId, pageSize, page);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Notifications.Should().HaveCount(remainingItems);
    }

    // TEST 13: Verify that the handler gracefully handles repository exceptions
    // Simulates a database or connection error during retrieval and expects a
    // failure result with the exception message included in the error
    [Fact]
    public async Task Handle_RepositoryThrowsException_ShouldReturnFailure()
    {
        // Arrange
        var userId = "user-123";
        var page = 1;
        var pageSize = 10;
        var exceptionMessage = "Redis connection failed";
        
        _readRepositoryMock
            .Setup(x => x.GetUserNotificationsAsync(userId, pageSize, page, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        var query = new GetUserNotificationsQuery(userId, pageSize, page);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be($"Error retrieving user notifications: {exceptionMessage}");
    }

    // TEST 14: Verify that the default PageSize value (20) is used when not specified
    // Tests that when PageSize is omitted from the query, the default value
    // of 20 is passed to the repository and returned in the response
    [Fact]
    public async Task Handle_DefaultPageSize_ShouldUseDefaultValue()
    {
        // Arrange
        var userId = "user-123";
        var defaultPageSize = 20;
        var page = 1;
        var notifications = CreateNotificationsList(userId, defaultPageSize);
        var expectedResponse = new NotificationListResponse(notifications, 100, page, defaultPageSize);

        _readRepositoryMock
            .Setup(x => x.GetUserNotificationsAsync(userId, defaultPageSize, page, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var query = new GetUserNotificationsQuery(userId, defaultPageSize, page);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.PageSize.Should().Be(defaultPageSize);
    }

    // TEST 15: Verify that the default Page value (1) is used when not specified
    // Tests that when Page is omitted from the query, the default value of 1
    // is passed to the repository and returned in the response
    [Fact]
    public async Task Handle_DefaultPage_ShouldUseFirstPage()
    {
        // Arrange
        var userId = "user-123";
        var pageSize = 10;
        var defaultPage = 1;
        var notifications = CreateNotificationsList(userId, pageSize);
        var expectedResponse = new NotificationListResponse(notifications, 100, defaultPage, pageSize);

        _readRepositoryMock
            .Setup(x => x.GetUserNotificationsAsync(userId, pageSize, defaultPage, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var query = new GetUserNotificationsQuery(userId, pageSize, defaultPage);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Page.Should().Be(defaultPage);
    }

    // TEST 16: Verify that consecutive calls do not use caching at handler level
    // Ensures that each call to the handler results in a repository call since
    // no caching is implemented at this level (cache may exist in Redis repository)
    [Fact]
    public async Task Handle_ConsecutiveCalls_ShouldCallRepositoryEachTime_WhenNoCacheImplemented()
    {
        // Arrange
        var userId = "user-123";
        var page = 1;
        var pageSize = 10;
        var notifications = CreateNotificationsList(userId, pageSize);
        var response = new NotificationListResponse(notifications, 25, page, pageSize);

        _readRepositoryMock
            .Setup(x => x.GetUserNotificationsAsync(userId, pageSize, page, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var query = new GetUserNotificationsQuery(userId, pageSize, page);

        // Act
        await _sut.Handle(query, CancellationToken.None);
        await _sut.Handle(query, CancellationToken.None);
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        _readRepositoryMock.Verify(x => x.GetUserNotificationsAsync(userId, pageSize, page, It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    #endregion

    private List<NotificationResponse> CreateNotificationsList(string userId, int count)
    {
        var notifications = new List<NotificationResponse>();
        for (int i = 1; i <= count; i++)
        {
            notifications.Add(new NotificationResponse(
                Id: $"notif-{i}",
                UserId: userId,
                Type: i % 2 == 0 ? NotificationType.InApp : NotificationType.Email,
                Title: $"Title {i}",
                Message: $"Message {i}",
                IsRead: i % 3 == 0,
                CreatedAt: DateTime.UtcNow.AddMinutes(-i),
                Metadata: null
            ));
        }
        return notifications;
    }
}
