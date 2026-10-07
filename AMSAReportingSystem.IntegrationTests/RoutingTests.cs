namespace AMSAReportingSystem.IntegrationTests;

public sealed class RoutingTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;

    public RoutingTests(TestAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetUnknownRoute_ReturnsClientHandledResponse()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/does-not-exist");

        // Assert
        Assert.NotNull(response);
    }

    [Fact]
    public async Task GetUnitReportsApi_WithoutUser_ReturnsUnauthorized()
    {
        using var factory = _factory.WithoutCurrentUser();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/reporting/unit-reports");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
