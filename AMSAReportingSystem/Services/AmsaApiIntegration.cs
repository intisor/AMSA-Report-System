using AMSAReportingSystem.Data.Entities;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AMSAReportingSystem.Services;

#region API Client Interface

/// <summary>
/// Client interface for AMSA API integration
/// Provides methods to call all 9 AMSA API endpoints
/// </summary>
public interface IAmsaApiClient
{
    /// <summary>
    /// Generate JWT token for member via app-to-app authentication
    /// POST /api/auth/token
    /// </summary>
    Task<Result<TokenResponse>> GenerateTokenAsync(string mkanId, IEnumerable<string> requestedScopes, CancellationToken ct = default);

    /// <summary>
    /// Get member details by Member ID, including roles and hierarchy
    /// GET /api/members/{memberId}
    /// </summary>
    Task<Result<MemberResponse>> GetMemberByIdAsync(int memberId, CancellationToken ct = default);

    /// <summary>
    /// Get member details by MKAN ID, including roles and hierarchy
    /// GET /api/members/mkan/{mkanid}
    /// </summary>
    Task<Result<MemberResponse>> GetMemberByMkanAsync(string mkanId, CancellationToken ct = default);

    /// <summary>
    /// Get all states with summary information
    /// GET /api/states
    /// </summary>
    Task<Result<List<StateResponse>>> GetAllStatesAsync(CancellationToken ct = default);

    /// <summary>
    /// Get single state details
    /// GET /api/states/{stateId}
    /// </summary>
    Task<Result<StateResponse>> GetStateByIdAsync(int stateId, CancellationToken ct = default);

    /// <summary>
    /// Get all units in a specific state
    /// GET /api/units/state/{stateId}
    /// </summary>
    Task<Result<List<UnitResponse>>> GetUnitsByStateAsync(int stateId, CancellationToken ct = default);

    /// <summary>
    /// Get single unit details with members and EXCO roles
    /// GET /api/units/{unitId}
    /// </summary>
    Task<Result<UnitResponse>> GetUnitByIdAsync(int unitId, CancellationToken ct = default);

    /// <summary>
    /// Register new application (Admin only)
    /// POST /api/auth/apps
    /// </summary>
    Task<Result<AppRegistrationResponse>> CreateAppAsync(CreateAppRequest request, CancellationToken ct = default);

    /// <summary>
    /// Get application registration details (Admin only)
    /// GET /api/auth/apps/{appId}
    /// </summary>
    Task<Result<AppRegistrationResponse>> GetAppAsync(string appId, CancellationToken ct = default);
}

#endregion

#region API Client Implementation

/// <summary>
/// Implementation of AMSA API client using HttpClient
/// Handles all 9 endpoint calls with error handling and logging
/// </summary>
public class AmsaApiClient : IAmsaApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AmsaApiClientOptions _options;
    private readonly ILogger<AmsaApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public AmsaApiClient(HttpClient httpClient, IOptions<AmsaApiClientOptions> options, ILogger<AmsaApiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public async Task<Result<TokenResponse>> GenerateTokenAsync(string mkanId, IEnumerable<string> requestedScopes, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating token for member {MkanId}", mkanId);

            var request = new
            {
                appId = _options.AppId,
                appSecret = _options.AppSecret,
                mkanId,
                requestedScopes = requestedScopes.ToList()
            };

            var response = await _httpClient.PostAsJsonAsync("/api/auth/token", request, cancellationToken: ct);
            return await HandleResponse<TokenResponse>(response, "GenerateToken");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating token for member {MkanId}", mkanId);
            return Result<TokenResponse>.Failure(ex.Message);
        }
    }

    public async Task<Result<MemberResponse>> GetMemberByIdAsync(int memberId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Fetching member by ID: {MemberId}", memberId);
            var response = await _httpClient.GetAsync($"/api/members/{memberId}", cancellationToken: ct);
            return await HandleResponse<MemberResponse>(response, "GetMemberById");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching member {MemberId}", memberId);
            return Result<MemberResponse>.Failure(ex.Message);
        }
    }

    public async Task<Result<MemberResponse>> GetMemberByMkanAsync(string mkanId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Fetching member by MKAN: {MkanId}", mkanId);
            var response = await _httpClient.GetAsync($"/api/members/mkan/{mkanId}", cancellationToken: ct);
            return await HandleResponse<MemberResponse>(response, "GetMemberByMkan");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching member {MkanId}", mkanId);
            return Result<MemberResponse>.Failure(ex.Message);
        }
    }

    public async Task<Result<List<StateResponse>>> GetAllStatesAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Fetching all states");
            var response = await _httpClient.GetAsync("/api/states", cancellationToken: ct);
            return await HandleResponse<List<StateResponse>>(response, "GetAllStates");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all states");
            return Result<List<StateResponse>>.Failure(ex.Message);
        }
    }

    public async Task<Result<StateResponse>> GetStateByIdAsync(int stateId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Fetching state by ID: {StateId}", stateId);
            var response = await _httpClient.GetAsync($"/api/states/{stateId}", cancellationToken: ct);
            return await HandleResponse<StateResponse>(response, "GetStateById");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching state {StateId}", stateId);
            return Result<StateResponse>.Failure(ex.Message);
        }
    }

    public async Task<Result<List<UnitResponse>>> GetUnitsByStateAsync(int stateId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Fetching units for state: {StateId}", stateId);
            var response = await _httpClient.GetAsync($"/api/units/state/{stateId}", cancellationToken: ct);
            return await HandleResponse<List<UnitResponse>>(response, "GetUnitsByState");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching units for state {StateId}", stateId);
            return Result<List<UnitResponse>>.Failure(ex.Message);
        }
    }

    public async Task<Result<UnitResponse>> GetUnitByIdAsync(int unitId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Fetching unit by ID: {UnitId}", unitId);
            var response = await _httpClient.GetAsync($"/api/units/{unitId}", cancellationToken: ct);
            return await HandleResponse<UnitResponse>(response, "GetUnitById");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching unit {UnitId}", unitId);
            return Result<UnitResponse>.Failure(ex.Message);
        }
    }

    public async Task<Result<AppRegistrationResponse>> CreateAppAsync(CreateAppRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating app registration: {AppId}", request.AppId);
            var response = await _httpClient.PostAsJsonAsync("/api/auth/apps", request, cancellationToken: ct);
            return await HandleResponse<AppRegistrationResponse>(response, "CreateApp");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating app registration: {AppId}", request.AppId);
            return Result<AppRegistrationResponse>.Failure(ex.Message);
        }
    }

    public async Task<Result<AppRegistrationResponse>> GetAppAsync(string appId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Fetching app registration: {AppId}", appId);
            var response = await _httpClient.GetAsync($"/api/auth/apps/{appId}", cancellationToken: ct);
            return await HandleResponse<AppRegistrationResponse>(response, "GetApp");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching app registration: {AppId}", appId);
            return Result<AppRegistrationResponse>.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Handle HTTP response and map to result
    /// </summary>
    private async Task<Result<T>> HandleResponse<T>(HttpResponseMessage response, string operationName)
    {
        try
        {
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<T>(json, _jsonOptions);
                _logger.LogInformation("Operation {OperationName} completed successfully", operationName);
                return Result<T>.Success(data!);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Operation {OperationName} failed with status {StatusCode}: {ErrorContent}",
                operationName, response.StatusCode, errorContent);

            return Result<T>.Failure($"API error: {response.StatusCode}", (int?)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing response for operation {OperationName}", operationName);
            return Result<T>.Failure($"Failed to process response: {ex.Message}");
        }
    }
}

#endregion

#region Configuration & Infrastructure

/// <summary>
/// Configuration options for AMSA API client integration
/// Binds from appsettings.json "AMSAApi" section
/// </summary>
public class AmsaApiClientOptions
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

    public void Reset()
    {
        IsChecked = false;
        IsConnected = false;
        LastCheckedAt = null;
        Message = "Checking AMSA API...";
    }
}

#endregion

#region Response Models

/// <summary>
/// Generic result wrapper for AMSA API responses
/// Handles both success and error scenarios
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public int? ErrorCode { get; set; }

    public static Result<T> Success(T data) => new() { IsSuccess = true, Data = data };
    public static Result<T> Failure(string message, int? code = null) => new() { IsSuccess = false, ErrorMessage = message, ErrorCode = code };
}

/// <summary>
/// JWT token response from /api/auth/token
/// </summary>
public class TokenResponse
{
    public string Token { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
}

/// <summary>
/// Member details response from /api/members/{id} and /api/members/mkan/{mkanid}
/// </summary>
public class MemberResponse
{
    public int MemberId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public required int MkanId { get; set; }

    /// <summary>
    /// Organizational hierarchy
    /// </summary>
    public required OrganizationHierarchyDto Unit { get; set; }

    /// <summary>
    /// Roles assigned to member at various levels
    /// Format: "DepartmentName:LevelType" e.g., "Taleem:Unit", "General:State", "President:National"
    /// </summary>
    public required List<RoleDto> Roles { get; set; }
}

/// <summary>
/// Organizational hierarchy for member
/// </summary>
public class OrganizationHierarchyDto
{
    public int UnitId { get; set; }
    public required string UnitName { get; set; }
    public required StateDto State { get; set; }
}

public class StateDto
{
    public int StateId { get; set; }
    public required string StateName { get; set; }
    public required NationalDto National { get; set; }
}

public class NationalDto
{
    public int NationalId { get; set; }
    public required string NationalName { get; set; }
}

/// <summary>
/// Role information with department and level
/// </summary>
public class RoleDto
{
    public required string DepartmentName { get; set; }
    public required LevelType LevelType { get; set; }
}

/// <summary>
/// State response from /api/states endpoints
/// </summary>
public class StateResponse
{
    public int StateId { get; set; }
    public required string StateName { get; set; }
    public int UnitCount { get; set; }
    public int MemberCount { get; set; }
    public int ExcoCount { get; set; }
}

/// <summary>
/// Unit response from /api/units endpoints
/// </summary>
public class UnitResponse
{
    public int UnitId { get; set; }
    public required string UnitName { get; set; }
    public int StateId { get; set; }
    public required string StateName { get; set; }
    public int MemberCount { get; set; }
    public int ExcoCount { get; set; }

    /// <summary>
    /// Members in this unit
    /// </summary>
    public List<MemberSummaryDto>? Members { get; set; }
}

/// <summary>
/// Summary of a member in organizational context
/// </summary>
public class MemberSummaryDto
{
    public int MemberId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required int MkanId { get; set; }
    public required string Email { get; set; }

    /// <summary>
    /// Roles at this unit
    /// </summary>
    public List<RoleDto>? Roles { get; set; }
}

/// <summary>
/// Request to create new application registration
/// </summary>
public class CreateAppRequest
{
    public required string AppId { get; set; }
    public required string AppName { get; set; }
    public required List<string> AllowedScopes { get; set; }
}

/// <summary>
/// Response from app registration endpoints
/// </summary>
public class AppRegistrationResponse
{
    public required string AppId { get; set; }
    public required string AppName { get; set; }
    public required string AppSecretHash { get; set; }
    public required List<string> AllowedScopes { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Error response from AMSA API (when applicable)
/// </summary>
public class ErrorResponse
{
    public required string Message { get; set; }
    public int? Code { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
}

#endregion
