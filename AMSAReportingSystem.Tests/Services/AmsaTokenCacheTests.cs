using AMSAReportingSystem.Services;

namespace AMSAReportingSystem.Tests.Services;

public sealed class AmsaTokenCacheTests
{
    [Fact]
    public void GetValidToken_WhenTokenIsFresh_ReturnsToken()
    {
        // Arrange
        var cache = new AmsaTokenCache();
        cache.SetToken("abc123", DateTime.UtcNow.AddHours(1), "1001");

        // Act
        var result = cache.GetValidToken();

        // Assert
        Assert.Equal("abc123", result);
    }

    [Fact]
    public void GetValidToken_WhenTokenIsExpired_ReturnsNull()
    {
        // Arrange
        var cache = new AmsaTokenCache();
        cache.SetToken("abc123", DateTime.UtcNow.AddMinutes(-1), "1001");

        // Act
        var result = cache.GetValidToken();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SetMkanId_StoresTheValue()
    {
        // Arrange
        var cache = new AmsaTokenCache();

        // Act
        cache.SetMkanId("1001");

        // Assert
        Assert.Equal("1001", cache.GetMkanId());
    }

    [Fact]
    public void Clear_RemovesCachedToken()
    {
        // Arrange
        var cache = new AmsaTokenCache();
        cache.SetToken("abc123", DateTime.UtcNow.AddHours(1), "1001");

        // Act
        cache.Clear();

        // Assert
        Assert.Null(cache.GetValidToken());
    }

    [Fact]
    public void CalculateEffectiveExpiration_WhenInputIsUtc_ReturnsEarlierUtcCutoff()
    {
        // Arrange
        var input = DateTime.UtcNow.AddHours(1);

        // Act
        var result = AmsaTokenCache.CalculateEffectiveExpiration(input);

        // Assert
        Assert.True(result <= input);
    }
}
