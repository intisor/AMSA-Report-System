using Microsoft.Extensions.Options;

namespace AMSAReportingSystem.Services;

/// <summary>
/// Core configuration and infrastructure for AMSA API client
/// Consolidates options, connection status, and health check service
/// </summary>

#region Configuration Options

/// <summary>
/// Configuration options for AMSA API client integration
/// Binds from appsettings.json "AMSAApi" section
/// </summary>
public class AMSAApiClientOptions
{
    public const string SectionName = "AMSAApi";

    /// <summary>
    /// Base URL of the AMSA API (e.g., https://api-dev.amsa.ng)
    /// </summary>
    public required string BaseUrl { get; set; }

    /// <summary>
    /// Application ID registered with AMSA API (e.g., "ReportingApp")
    /// </summary>
    public required string AppId { get; set; }

    /// <summary>
    /// Application secret for authentication (stored securely in User Secrets for dev)
    /// </summary>
    public required string AppSecret { get; set; }

    /// <summary>
    /// HTTP request timeout in seconds (default: 300)
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Resilience policy configuration for retry and circuit breaker
    /// </summary>
    public required ResiliencePolicyOptions ResiliencePolicy { get; set; }
}

/// <summary>
/// Resilience policy settings for Polly
/// </summary>
public class ResiliencePolicyOptions
{
    /// <summary>
    /// Maximum number of retry attempts (default: 3)
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Initial delay between retries in milliseconds (grows exponentially)
    /// </summary>
    public int DelayMilliseconds { get; set; } = 1000;

    /// <summary>
    /// Number of failures before circuit breaker opens (default: 5)
    /// </summary>
    public int CircuitBreakerThreshold { get; set; } = 5;

    /// <summary>
    /// Duration circuit breaker stays open in seconds (default: 30)
    /// </summary>
    public int CircuitBreakerDurationSeconds { get; set; } = 30;
}

#endregion

#region Connection Status & Monitoring

/// <summary>
/// Real-time connection status for AMSA API
/// Tracks health checks and provides status information to UI
/// </summary>
public class AMSAApiConnectionStatus
{
    public bool IsChecked { get; private set; }
    public bool IsConnected { get; private set; }
    public string Message { get; private set; } = "Checking AMSA API...";
    public DateTimeOffset? LastCheckedAt { get; private set; }

    public void MarkConnected(string? message = null)
    {
        IsChecked = true;
        IsConnected = true;
        LastCheckedAt = DateTimeOffset.UtcNow;
        Message = string.IsNullOrWhiteSpace(message) ? "Connected to AMSA API" : message;
    }

    public void MarkFailed(string? message = null)
    {
        IsChecked = true;
        IsConnected = false;
        LastCheckedAt = DateTimeOffset.UtcNow;
        Message = string.IsNullOrWhiteSpace(message) ? "AMSA API unavailable" : message;
    }
}

#endregion

#region Startup Health Check Service

/// <summary>
/// Background service that verifies AMSA API connectivity at application startup
/// Logs connection status and updates AMSAApiConnectionStatus for UI display
/// </summary>
public class AMSAApiStartupHealthCheckService : IHostedService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AMSAApiClientOptions _options;
    private readonly AMSAApiConnectionStatus _connectionStatus;
    private readonly ILogger<AMSAApiStartupHealthCheckService> _logger;

    public AMSAApiStartupHealthCheckService(
        IHttpClientFactory httpClientFactory,
        AMSAApiConnectionStatus connectionStatus,
        IOptions<AMSAApiClientOptions> options,
        ILogger<AMSAApiStartupHealthCheckService> logger)
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
            client.Timeout = TimeSpan.FromSeconds(Math.Min(300, _options.RequestTimeoutSeconds));

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
                "AMSA API connectivity check failed at startup. BaseUrl: {BaseUrl}. Check API availability, DNS, and appsettings AMSAApi configuration.",
                _options.BaseUrl);
            _connectionStatus.MarkFailed("AMSA API unavailable");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

#endregion
