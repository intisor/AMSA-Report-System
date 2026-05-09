using AMSAReportingSystem.Components.Pages;
using AMSAReportingSystem.Data.Entities;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;
using System.Text.Json;

namespace AMSAReportingSystem.Services;

#region AmsaAuthService

/// <summary>
/// Authentication service that integrates with AMSA API
/// Handles token generation and member authentication via AMSA API
/// </summary>
public class AmsaAuthService
{
    private readonly IAmsaApiClient _apiClient;
    private readonly ILogger<AmsaAuthService> _logger;
    private AmsaTokenCache _tokenCache;
    private string? _cachedToken;
    private DateTime _tokenExpiry;

    public AmsaAuthService(IAmsaApiClient apiClient, ILogger<AmsaAuthService> logger, AmsaTokenCache tokenCache)
    {
        _apiClient = apiClient;
        _logger = logger;
        _tokenCache = tokenCache;
        _tokenExpiry = DateTime.UtcNow;
    }

    /// <summary>
    /// Authenticate member and get JWT token from AMSA API
    /// Uses app-to-app authentication model
    /// </summary>
    public async Task<AuthContext?> AuthenticateAsync(string mkanId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Authenticating member {MkanId} via AMSA API", mkanId);

            // Request token from AMSA API with standard scopes
            var scopes = new[] { "read:members", "read:statistics", "read:organization" };
            var tokenResult = await _apiClient.GenerateTokenAsync(mkanId, scopes, ct);

            if (!tokenResult.IsSuccess || tokenResult.Data == null)
            {
                _logger.LogWarning("Token generation failed for member {MkanId}: {Error}", mkanId, tokenResult.ErrorMessage);
                return null;
            }

            _tokenCache.SetMkanId(mkanId);

            // Get member details from AMSA API
            var memberResult = await _apiClient.GetMemberByMkanAsync(mkanId, ct);

            if (!memberResult.IsSuccess || memberResult.Data == null)
            {
                _logger.LogWarning("Failed to fetch member details for {MkanId}: {Error}", mkanId, memberResult.ErrorMessage);
                return null;
            }

            var member = memberResult.Data;

            // Create auth context with token and member details
            var authContext = new AuthContext
            {
                MemberId = member.MemberId,
                MkanId = member.MkanId,
                FirstName = member.FirstName,
                LastName = member.LastName,
                Email = member.Email,
                UnitId = member.Unit.UnitId,
                UnitName = member.Unit.UnitName,
                StateId = member.Unit.State.StateId,
                StateName = member.Unit.State.StateName,
                NationalId = member.Unit.State.National.NationalId,
                NationalName = member.Unit.State.National.NationalName,
                Token = tokenResult.Data.Token,
                TokenExpiry = AmsaTokenCache.CalculateEffectiveExpiration(tokenResult.Data.ExpiresAt ?? DateTime.UtcNow.AddHours(1)),
                Roles = member.Roles.Select(r => $"{r.DepartmentName}:{r.LevelType}").ToList()
            };

            // Parse roles into structured data and infer dashboard access from level types.
            authContext.ParsedRoles = authContext.Roles.Select(role => ParseRole(role)).ToList();
            authContext.Dashboards = authContext.ParsedRoles
                .Select(role => DetermineDashboard(role.LevelType))
                .Where(dashboard => dashboard is not null)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Cache token for potential reuse
            _cachedToken = authContext.Token;
            _tokenExpiry = authContext.TokenExpiry;

            _logger.LogInformation("Successfully authenticated member {FirstName} {LastName} ({MkanId})",
                member.FirstName, member.LastName, mkanId);

            return authContext;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating member {MkanId}", mkanId);
            return null;
        }
    }

    /// <summary>
    /// Get cached token if still valid
    /// </summary>
    public string? GetCachedToken()
    {
        if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
        {
            return _cachedToken;
        }

        _cachedToken = null;
        return null;
    }

    /// <summary>
    /// Refresh authentication context (re-authenticate with AMSA API)
    /// </summary>
    public async Task<AuthContext?> RefreshAsync(string mkanId, CancellationToken ct = default)
    {
        _logger.LogInformation("Refreshing authentication for member {MkanId}", mkanId);
        return await AuthenticateAsync(mkanId, ct);
    }

    /// <summary>
    /// Parses roles in the format "DepartmentName:LevelType" into structured data.
    /// </summary>
    private (string DepartmentName, LevelType LevelType) ParseRole(string role)
    {
        var parts = role.Split(':', 2);
        if (parts.Length != 2)
        {
            throw new FormatException($"Invalid role format: {role}");
        }

        if (!Enum.TryParse<LevelType>(parts[1], ignoreCase: true, out var levelType))
        {
            throw new FormatException($"Invalid level type in role: {role}");
        }

        return (parts[0], levelType);
    }

    /// <summary>
    /// Parses and categorizes roles for the authenticated member.
    /// </summary>
    private static string DetermineDashboard(LevelType levelType)
    {
        return levelType switch
        {
            LevelType.National => "NationalDashboard",
            LevelType.State => "StateDashboard",
            _ => "UnitDashboard"
        };
    }
}

#endregion

#region AMSAAuthStateProvider

/// <summary>
/// Custom auth state provider for the AMSA Reporting System
/// Integrates with AMSA API for real authentication
/// Implements Blazor's AuthenticationStateProvider for framework integration
/// </summary>
public class AMSAAuthStateProvider(AmsaAuthService authService, IJSRuntime jsRuntime, ILogger<AMSAAuthStateProvider> logger) : AuthenticationStateProvider
{
    private const string AuthStorageKey = "amsa.auth.context";
    private readonly AmsaAuthService _authService = authService;
    private readonly IJSRuntime _jsRuntime = jsRuntime;
    private readonly ILogger<AMSAAuthStateProvider> _logger = logger;
    private AuthContext? _currentUser;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Try to restore from session/localStorage
        _currentUser ??= await RestoreAuthStateAsync();

        if (_currentUser != null && _currentUser.IsTokenValid)
        {
            return new AuthenticationState(CreateClaimsPrincipal(_currentUser));
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    /// <summary>
    /// Authenticate user with AMSA API
    /// </summary>
    public async Task LoginAsync(string mkanId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Login attempt for MKAN ID: {MkanId}", mkanId);

            var result = await _authService.AuthenticateAsync(mkanId, ct);

            if (result == null)
            {
                _logger.LogWarning("Authentication failed for MKAN ID: {MkanId}", mkanId);
                throw new InvalidOperationException("Authentication failed. Please check your MKAN ID.");
            }

            _currentUser = result;
            await PersistAuthStateAsync(result);
            _logger.LogInformation("User {FirstName} {LastName} logged in successfully", result.FirstName, result.LastName);

            // Notify Blazor of auth state change
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for MKAN ID: {MkanId}", mkanId);
            throw;
        }
    }

    /// <summary>
    /// Logout current user
    /// </summary>
    public async Task LogoutAsync()
    {
        if (_currentUser != null)
        {
            _logger.LogInformation("User {FirstName} {LastName} logged out", _currentUser.FirstName, _currentUser.LastName);
        }

        _currentUser = null;
        await ClearAuthStateAsync();
        await Task.CompletedTask;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    /// <summary>
    /// Get current authenticated user context
    /// </summary>
    public AuthContext? GetCurrentUser() => _currentUser;

    /// <summary>
    /// Restore auth state from persistent storage
    /// Currently in-memory only; extend with localStorage/session as needed
    /// </summary>
    private async Task<AuthContext?> RestoreAuthStateAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", AuthStorageKey);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var user = JsonSerializer.Deserialize<AuthContext>(json);
            if (user is null || !user.IsTokenValid)
            {
                await ClearAuthStateAsync();
                return null;
            }

            return user;
        }
        catch (InvalidOperationException)
        {
            // JS runtime may not be available yet in early render.
            return null;
        }
        catch (JSException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to restore auth state from local storage");
            return null;
        }
    }

    private async Task PersistAuthStateAsync(AuthContext context)
    {
        try
        {
            var json = JsonSerializer.Serialize(context);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AuthStorageKey, json);
        }
        catch (InvalidOperationException)
        {
            // JS runtime may not be available yet in early render.
        }
        catch (JSException)
        {
            // Ignore storage failures for now.
        }
    }

    private async Task ClearAuthStateAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AuthStorageKey);
        }
        catch (InvalidOperationException)
        {
        }
        catch (JSException)
        {
        }
    }

    /// <summary>
    /// Create claims principal from auth context
    /// Maps member data to .NET claims for authorization
    /// </summary>
    private static ClaimsPrincipal CreateClaimsPrincipal(AuthContext user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.MemberId.ToString()),
            new Claim(ClaimTypes.Name, user.GetDisplayName()),
            new Claim("mkanId", user.MkanId.ToString()),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName),
            new Claim("email", user.Email),
            new Claim("unitId", user.UnitId.ToString()),
            new Claim("unitName", user.UnitName),
            new Claim("stateId", user.StateId.ToString()),
            new Claim("stateName", user.StateName),
            new Claim("token", user.Token),
        };

        // Add role claims for each role the user has
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, "amsa-auth");
        return new ClaimsPrincipal(identity);
    }
}

#endregion
