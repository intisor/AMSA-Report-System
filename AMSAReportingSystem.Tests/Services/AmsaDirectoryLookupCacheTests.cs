using AMSAReportingSystem.Services;
using Moq;

namespace AMSAReportingSystem.Tests.Services;

public sealed class AmsaDirectoryLookupCacheTests
{
    [Fact]
    public async Task GetStateNameAsync_WhenStateExists_ReturnsStateName()
    {
        var apiClient = new Mock<IAmsaApiClient>(MockBehavior.Strict);
        apiClient.Setup(client => client.GetStateByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Data.Entities.StateResponse>.Success(new Data.Entities.StateResponse { StateId = 10, StateName = "Kaduna" }));
        var cache = new AmsaDirectoryLookupCache(apiClient.Object);

        var result = await cache.GetStateNameAsync(10);

        Assert.Equal("Kaduna", result);
        apiClient.Verify(client => client.GetStateByIdAsync(10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStateNameAsync_WhenCached_ReturnsCachedValue()
    {
        var apiClient = new Mock<IAmsaApiClient>(MockBehavior.Strict);
        apiClient.Setup(client => client.GetStateByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Data.Entities.StateResponse>.Success(new Data.Entities.StateResponse { StateId = 10, StateName = "Kaduna" }));
        var cache = new AmsaDirectoryLookupCache(apiClient.Object);

        var first = await cache.GetStateNameAsync(10);
        var second = await cache.GetStateNameAsync(10);

        Assert.Equal("Kaduna", first);
        Assert.Equal("Kaduna", second);
        apiClient.Verify(client => client.GetStateByIdAsync(10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUnitNameAsync_WhenLookupFails_ReturnsFallbackName()
    {
        var apiClient = new Mock<IAmsaApiClient>(MockBehavior.Strict);
        apiClient.Setup(client => client.GetUnitByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Data.Entities.UnitResponse>.Failure("missing"));
        var cache = new AmsaDirectoryLookupCache(apiClient.Object);

        var result = await cache.GetUnitNameAsync(7);

        Assert.Equal("Unit 7", result);
    }
}
