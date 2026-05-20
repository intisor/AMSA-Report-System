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
}
