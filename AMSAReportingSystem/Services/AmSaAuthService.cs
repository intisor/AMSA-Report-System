namespace AMSAReportingSystem.Services;

/// <summary>
/// Authentication service that integrates with AMSA API
/// Replaces MockAuthService with real app-to-app authentication
/// </summary>
public class AmSaAuthService
{
    private readonly IAmSaApiClient _apiClient;
    private readonly ILogger<AmSaAuthService> _logger;
    private string? _cachedToken;
    private DateTime _tokenExpiry;

    public AmSaAuthService(IAmSaApiClient apiClient, ILogger<AmSaAuthService> logger)
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
            var scopes = new[] { "member:read", "organization:read" };
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
                UnitId = member.Hierarchy.UnitId,
                UnitName = member.Hierarchy.UnitName,
                StateId = member.Hierarchy.StateId,
                StateName = member.Hierarchy.StateName,
                Token = tokenResult.Data.Token,
                TokenExpiry = DateTime.UtcNow.AddSeconds(tokenResult.Data.ExpiresIn),
                Roles = member.Roles.Select(r => $"{r.DepartmentName}:{r.LevelType}").ToList()
            };

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
}

/// <summary>
/// Authentication context containing member details and JWT token
/// Maps from AMSA API member response to Reporting System user context
/// </summary>
public class AuthContext
{
    public int MemberId { get; set; }
    public required string MkanId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public int UnitId { get; set; }
    public required string UnitName { get; set; }
    public int StateId { get; set; }
    public required string StateName { get; set; }
    public required string Token { get; set; }
    public DateTime TokenExpiry { get; set; }

    /// <summary>
    /// Roles in format "DepartmentName:LevelType"
    /// </summary>
    public required List<string> Roles { get; set; }

    /// <summary>
    /// Check if token is still valid
    /// </summary>
    public bool IsTokenValid => DateTime.UtcNow < TokenExpiry;

    /// <summary>
    /// Check if user is authenticated (has valid token)
    /// </summary>
    public bool IsAuthenticated => IsTokenValid && !string.IsNullOrEmpty(Token);

    /// <summary>
    /// Get display name for member
    /// </summary>
    public string GetDisplayName() => $"{FirstName} {LastName}";

    /// <summary>
    /// Get full name
    /// </summary>
    public string FullName => GetDisplayName();

    /// <summary>
    /// Check if member has specific role (case-insensitive)
    /// </summary>
    public bool HasRole(string role) => Roles.Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Check if user is a department officer
    /// </summary>
    public bool IsDepartmentOfficer => Roles.Any(r => r.EndsWith(":DepartmentOfficer", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Check if user is a unit president
    /// </summary>
    public bool IsUnitPresident => Roles.Any(r => r.EndsWith(":UnitPresident", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Check if user is a state general secretary
    /// </summary>
    public bool IsStateGS => Roles.Any(r => r.EndsWith(":StateGS", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Check if user is a state president
    /// </summary>
    public bool IsStatePresident => Roles.Any(r => r.EndsWith(":StatePresident", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Check if user is national general secretary
    /// </summary>
    public bool IsNationalGS => Roles.Any(r => r.EndsWith(":NationalGS", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Check if user is in national leadership
    /// </summary>
    public bool IsNationalLeadership => Roles.Any(r => r.EndsWith(":National", StringComparison.OrdinalIgnoreCase) || 
                                                        r.EndsWith(":NationalGS", StringComparison.OrdinalIgnoreCase) ||
                                                        r.EndsWith(":NationalPresident", StringComparison.OrdinalIgnoreCase));
}
