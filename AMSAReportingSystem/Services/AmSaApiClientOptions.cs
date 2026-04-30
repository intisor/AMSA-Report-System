namespace AMSAReportingSystem.Services;

/// <summary>
/// Configuration options for AMSA API client integration
/// Binds from appsettings.json "AmSaApi" section
/// </summary>
public class AmSaApiClientOptions
{
    public const string SectionName = "AmSaApi";

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
