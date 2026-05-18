using AMSAReportingSystem.Services;

namespace AMSAReportingSystem.Tests.Services;

public sealed class AmsaTokenCacheTests
{
    [Fact]
    public void GetValidToken_WhenTokenIsFresh_ReturnsToken()
    {
        var cache = new AmsaTokenCache();
        cache.SetToken("abc123", DateTime.UtcNow.AddHours(1), "1001");

        var result = cache.GetValidToken();

        Assert.Equal("abc123", result);
    }

    [Fact]
    public void GetValidToken_WhenTokenIsExpired_ReturnsNull()
    {
        var cache = new AmsaTokenCache();
        cache.SetToken("abc123", DateTime.UtcNow.AddMinutes(-1), "1001");

        var result = cache.GetValidToken();

        Assert.Null(result);
    }

    [Fact]
    public void SetMkanId_StoresTheValue()
    {
        var cache = new AmsaTokenCache();

        cache.SetMkanId("1001");

        Assert.Equal("1001", cache.GetMkanId());
    }

    [Fact]
    public void Clear_RemovesCachedToken()
    {
        var cache = new AmsaTokenCache();
        cache.SetToken("abc123", DateTime.UtcNow.AddHours(1), "1001");

        cache.Clear();

        Assert.Null(cache.GetValidToken());
    }

    [Fact]
    public void CalculateEffectiveExpiration_WhenInputIsUtc_ReturnsEarlierUtcCutoff()
    {
        var input = DateTime.UtcNow.AddHours(1);

        var result = AmsaTokenCache.CalculateEffectiveExpiration(input);

        Assert.True(result <= input);
    }
}
