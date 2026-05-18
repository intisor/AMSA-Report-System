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
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetLoginPage_ReturnsSuccessfulResponse()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/login");

        response.EnsureSuccessStatusCode();
    }
}
