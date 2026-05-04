using AMSAReportingSystem.Components.Pages;
using AMSAReportingSystem.Data.Entities;

namespace AMSAReportingSystem.Services;

/// <summary>
/// Authentication service that integrates with AMSA API
/// Replaces MockAuthService with real app-to-app authentication
/// </summary>
public class AMSAAuthService
{
    private readonly IAmSaApiClient _apiClient;
    private readonly ILogger<AMSAAuthService> _logger;
    private string? _cachedToken;
    private DateTime _tokenExpiry;

    public AMSAAuthService(IAmSaApiClient apiClient, ILogger<AMSAAuthService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
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
            var scopes = new[] {"read:members", "read:statistics", "read:organization"};
            var tokenResult = await _apiClient.GenerateTokenAsync(mkanId, scopes, ct);

            if (!tokenResult.IsSuccess || tokenResult.Data == null)
            {
                _logger.LogWarning("Token generation failed for member {MkanId}: {Error}", mkanId, tokenResult.ErrorMessage);
                return null;
            }

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
                TokenExpiry = DateTime.UtcNow.AddSeconds( 3600),
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
