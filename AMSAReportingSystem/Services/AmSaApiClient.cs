using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace AMSAReportingSystem.Services;

/// <summary>
/// Implementation of AMSA API client using HttpClient
/// Handles all 9 endpoint calls with error handling and logging
/// </summary>
public class AmSaApiClient : IAmSaApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AmSaApiClientOptions _options;
    private readonly ILogger<AmSaApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public AmSaApiClient(HttpClient httpClient, IOptions<AmSaApiClientOptions> options, ILogger<AmSaApiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
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
