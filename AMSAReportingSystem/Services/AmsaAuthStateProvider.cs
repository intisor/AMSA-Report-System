using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;
using System.Text.Json;

namespace AMSAReportingSystem.Services;

/// <summary>
/// Custom auth state provider for the AMSA Reporting System
/// Integrates with AMSA API for real authentication
/// </summary>
public class AMSAAuthStateProvider : AuthenticationStateProvider
{
    private const string AuthStorageKey = "amsa.auth.context";
    private readonly AMSAAuthService _authService;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<AMSAAuthStateProvider> _logger;
    private AuthContext? _currentUser;

    public AMSAAuthStateProvider(AMSAAuthService authService, IJSRuntime jsRuntime, ILogger<AMSAAuthStateProvider> logger)
    {
        _authService = authService;
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_currentUser == null)
        {
            // Try to restore from session/localStorage
            _currentUser = await RestoreAuthStateAsync();
        }

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

