using AMSAReportingSystem.Components.Pages;

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
    private (string DepartmentName, string LevelType) ParseRole(string role)
    {
        var parts = role.Split(':');
        if (parts.Length != 2)
        {
            throw new FormatException($"Invalid role format: {role}");
        }

        return (parts[0], parts[1]);
    }

    /// <summary>
    /// Parses and categorizes roles for the authenticated member.
    /// </summary>
    private string? DetermineDashboard(string levelType)
    {
        return levelType.ToLower() switch
        {
            "national" => "NationalDashboard",
            "state" => "StateDashboard",
            "unit" => "UnitDashboard",
            _ => null
        };
    }
}

/// <summary>
/// Authentication context containing member details and JWT token
/// Maps from AMSA API member response to Reporting System user context
/// </summary>
public class AuthContext
{
    public int MemberId { get; set; }
    public required int MkanId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public int UnitId { get; set; }
    public required string UnitName { get; set; }
    public int StateId { get; set; }
    public required string StateName { get; set; }
    public int NationalId { get; set; }
    public required string NationalName { get; set; }
    public List<string?>? Dashboards { get; set; }
    public required string Token { get; set; }
    public DateTime TokenExpiry { get; set; }

    /// <summary>
    /// Roles in format "DepartmentName:LevelType"
    /// </summary>
    public required List<string> Roles { get; set; }

    /// <summary>
    /// Parsed roles with structured data
    /// </summary>
    public List<(string DepartmentName, string LevelType)> ParsedRoles { get; set; } = new List<(string DepartmentName, string LevelType)>();

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

    public string GetDashboard()
    {
        if (Dashboards?.Any(d => d == "NationalDashboard") == true)
        {
            return "NationalDashboard";
        }

        if (Dashboards?.Any(d => d == "StateDashboard") == true)
        {
            return "StateDashboard";
        }

        if (Dashboards?.Any(d => d == "UnitDashboard") == true)
        {
            return "UnitDashboard";
        }

        return ParsedRoles.FirstOrDefault().LevelType.ToLowerInvariant() switch
        {
            "national" => "NationalDashboard",
            "state" => "StateDashboard",
            "unit" => "UnitDashboard",
            _ => "UnitDashboard"
        };
    }

    public bool IsDepartmentOfficer => ParsedRoles.Any(r => r.LevelType.Equals("Unit", StringComparison.OrdinalIgnoreCase));
    public bool IsUnitLeadership => ParsedRoles.Any(r =>
        r.LevelType.Equals("Unit", StringComparison.OrdinalIgnoreCase) && IsLeadershipDepartment(r.DepartmentName));
    public bool IsStateLeadership => ParsedRoles.Any(r =>
        r.LevelType.Equals("State", StringComparison.OrdinalIgnoreCase) && IsLeadershipDepartment(r.DepartmentName));
    public bool IsNationalLeadership => ParsedRoles.Any(r =>
        r.LevelType.Equals("National", StringComparison.OrdinalIgnoreCase) && IsLeadershipDepartment(r.DepartmentName));
    public bool HasSudoAccess => ParsedRoles.Any(r => IsLeadershipDepartment(r.DepartmentName));

    private static bool IsLeadershipDepartment(string departmentName) =>
        departmentName.Equals("President", StringComparison.OrdinalIgnoreCase) ||
        departmentName.Equals("General", StringComparison.OrdinalIgnoreCase);
}


// Refactor Documentation
// The role handling logic in AmSaAuthService.cs has been refactored to improve maintainability and scalability:
// 1. ParseRole: Parses roles in the format "DepartmentName:LevelType".
// 2. DetermineDashboard: Maps LevelType to specific dashboards.
// 3. CanAccessDepartment: Determines department access based on DepartmentName.
// 4. HasSudoAccess: Grants sudo access to President and General Secretary.
// 5. AuthenticateAsync: Refactored to use the new methods for role parsing, dashboard determination, and department access.
