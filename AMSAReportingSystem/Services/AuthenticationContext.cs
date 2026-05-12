using AMSAReportingSystem.Data.Entities;

namespace AMSAReportingSystem.Services;

/// <summary>
/// Dashboard role for UI routing and authorization
/// Represents the primary dashboard a user should access
/// </summary>
public enum DashboardRole
{
    /// <summary>National leadership dashboard</summary>
    National,

    /// <summary>State leadership dashboard</summary>
    State,

    /// <summary>Unit leadership dashboard (unit president level)</summary>
    UnitLeadership,

    /// <summary>Department officer dashboard (regular unit members)</summary>
    DepartmentOfficer
}

/// <summary>
/// Authentication context containing member details and JWT token
/// Maps from AMSA API member response to Reporting System user context
/// Separated from AmSaAuthService for single responsibility
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
    public List<string>? Dashboards { get; set; }
    public required string Token { get; set; }
    public DateTime TokenExpiry { get; set; }

    /// <summary>
    /// Roles in format "DepartmentName:LevelType"
    /// </summary>
    public required List<string> Roles { get; set; }

    /// <summary>
    /// Parsed roles with structured data
    /// </summary>
    public List<(string DepartmentName, LevelType LevelType)> ParsedRoles { get; set; } = new();

    #region Token & Authentication Status

    /// <summary>
    /// Check if token is still valid
    /// </summary>
    public bool IsTokenValid => DateTime.UtcNow < TokenExpiry;

    /// <summary>
    /// Check if user is authenticated (has valid token)
    /// </summary>
    public bool IsAuthenticated => IsTokenValid && !string.IsNullOrEmpty(Token);

    #endregion

    #region Display Properties

    /// <summary>
    /// Get display name for member
    /// </summary>
    public string GetDisplayName() => $"{FirstName} {LastName}";

    /// <summary>
    /// Get full name
    /// </summary>
    public string FullName => GetDisplayName();

    #endregion

    #region Role & Authorization Helpers

    /// <summary>
    /// Check if member has specific role (case-insensitive)
    /// </summary>
    public bool HasRole(string role) => Roles.Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Determine primary dashboard access based on roles
    /// Priority: National > State > Unit
    /// </summary>
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

        return ParsedRoles.FirstOrDefault().LevelType switch
        {
            LevelType.National => "NationalDashboard",
            LevelType.State => "StateDashboard",
            LevelType.Unit => "UnitDashboard",
            _ => "UnitDashboard"
        };
    }

    #endregion

    #region Role Type Checks

    /// <summary>
    /// Is member a department officer at any level
    /// </summary>
    public bool IsDepartmentOfficer => ParsedRoles.Any(r => r.LevelType == LevelType.Unit);

    /// <summary>
    /// Is member unit-level leadership (President or department head)
    /// </summary>
    public bool IsUnitLeadership => ParsedRoles.Any(r =>
        r.LevelType == LevelType.Unit && IsLeadershipDepartment(r.DepartmentName, r.LevelType));

    /// <summary>
    /// Is member state-level leadership
    /// </summary>
    public bool IsStateLeadership => ParsedRoles.Any(r =>
        r.LevelType == LevelType.State && IsLeadershipDepartment(r.DepartmentName, r.LevelType));

    /// <summary>
    /// Is member national-level leadership
    /// </summary>
    public bool IsNationalLeadership => ParsedRoles.Any(r =>
        r.LevelType == LevelType.National && IsLeadershipDepartment(r.DepartmentName, r.LevelType));

    /// <summary>
    /// Has sudo access (is any level of leadership)
    /// </summary>
    public bool HasSudoAccess => ParsedRoles.Any(r => IsLeadershipDepartment(r.DepartmentName, r.LevelType));

    #endregion

    #region Department & Leadership Resolution

    /// <summary>
    /// Check if department represents leadership position
    /// VP roles only valid at national level
    /// </summary>
    private static bool IsLeadershipDepartment(string departmentName, LevelType levelType)
    {
        var normalized = NormalizeDepartmentName(departmentName);

        // VP* roles are valid only at National level.
        if (IsVpDepartment(normalized) && levelType != LevelType.National)
        {
            return false;
        }

        return normalized is "president"
            or "general"
            or "vicepresident"
            or "vpadmin"
            or "vpnorth"
            or "vpsouthsouthsoutheast"
            or "vpsouthwest";
    }

    /// <summary>
    /// Normalize department name to lowercase, removing spaces and special characters
    /// Maps assistant roles to their primary departments
    /// </summary>
    private static string NormalizeDepartmentName(string name)
    {
        var normalized = name.Trim().ToLowerInvariant()
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace("/", string.Empty);

        return normalized;
    }

    /// <summary>
    /// Check if department is a Vice President (regional or functional) role
    /// </summary>
    private static bool IsVpDepartment(string normalizedDepartment) =>
        normalizedDepartment is "vicepresident" or "vpadmin" or "vpnorth" or "vpsouthsouthsoutheast" or "vpsouthwest";

    #endregion
}
