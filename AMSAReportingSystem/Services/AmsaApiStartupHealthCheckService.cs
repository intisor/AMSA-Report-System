using Microsoft.Extensions.Options;

namespace AMSAReportingSystem.Services;

public class AmsaApiStartupHealthCheckService : IHostedService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AmSaApiClientOptions _options;
    private readonly AmsaApiConnectionStatus _connectionStatus;
    private readonly ILogger<AmsaApiStartupHealthCheckService> _logger;

    public AmsaApiStartupHealthCheckService(
        IHttpClientFactory httpClientFactory,
        AmsaApiConnectionStatus connectionStatus,
        IOptions<AmSaApiClientOptions> options,
        ILogger<AmsaApiStartupHealthCheckService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _connectionStatus = connectionStatus;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(Math.Min(10, _options.RequestTimeoutSeconds));

            using var request = new HttpRequestMessage(HttpMethod.Get, "/");
            using var response = await client.SendAsync(request, cancellationToken);

            _logger.LogInformation(
                "AMSA API connectivity check successful. BaseUrl: {BaseUrl}, StatusCode: {StatusCode}",
                _options.BaseUrl,
                (int)response.StatusCode);
            _connectionStatus.MarkConnected();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "AMSA API connectivity check failed at startup. BaseUrl: {BaseUrl}. Check API availability, DNS, and appsettings AmSaApi configuration.",
                _options.BaseUrl);
            _connectionStatus.MarkFailed("AMSA API unavailable");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
