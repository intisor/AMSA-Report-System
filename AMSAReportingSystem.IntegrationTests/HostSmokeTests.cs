namespace AMSAReportingSystem.IntegrationTests;

public sealed class HostSmokeTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;

    public HostSmokeTests(TestAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetRootPage_ReturnsSuccessfulResponse()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetLoginPage_ReturnsSuccessfulResponse()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/login");

        // Assert
        response.EnsureSuccessStatusCode();
    }
}
