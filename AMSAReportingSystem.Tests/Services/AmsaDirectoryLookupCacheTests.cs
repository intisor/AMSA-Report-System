using AMSAReportingSystem.Services;
using Moq;

namespace AMSAReportingSystem.Tests.Services;

public sealed class AmsaDirectoryLookupCacheTests
{
    [Fact]
    public async Task GetStateNameAsync_WhenStateExists_ReturnsStateName()
    {
        // Arrange
        var apiClient = new Mock<IAmsaApiClient>(MockBehavior.Strict);
        apiClient.Setup(client => client.GetStateByIdAsync(10, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Result<StateResponse>.Success(new StateResponse { StateId = 10, StateName = "Kaduna" })));
        var cache = new AmsaDirectoryLookupCache(apiClient.Object);

        // Act
        var result = await cache.GetStateNameAsync(10);

        // Assert
        Assert.Equal("Kaduna", result);
        apiClient.Verify(client => client.GetStateByIdAsync(10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStateNameAsync_WhenCached_ReturnsCachedValue()
    {
        // Arrange
        var apiClient = new Mock<IAmsaApiClient>(MockBehavior.Strict);
        apiClient.Setup(client => client.GetStateByIdAsync(10, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Result<StateResponse>.Success(new StateResponse { StateId = 10, StateName = "Kaduna" })));
        var cache = new AmsaDirectoryLookupCache(apiClient.Object);

        // Act
        var first = await cache.GetStateNameAsync(10);
        var second = await cache.GetStateNameAsync(10);

        // Assert
        Assert.Equal("Kaduna", first);
        Assert.Equal("Kaduna", second);
        apiClient.Verify(client => client.GetStateByIdAsync(10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUnitNameAsync_WhenLookupFails_ReturnsFallbackName()
    {
        // Arrange
        var apiClient = new Mock<IAmsaApiClient>(MockBehavior.Strict);
        apiClient.Setup(client => client.GetUnitByIdAsync(7, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Result<UnitResponse>.Failure("missing")));
        var cache = new AmsaDirectoryLookupCache(apiClient.Object);

        // Act
        var result = await cache.GetUnitNameAsync(7);

        // Assert
        Assert.Equal("Unit 7", result);
    }
}
