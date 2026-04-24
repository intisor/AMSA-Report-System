namespace AMSAReportingSystem.Services;

/// <summary>
/// Client interface for AMSA API integration
/// Provides methods to call all 9 AMSA API endpoints
/// </summary>
public interface IAmSaApiClient
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
    public int? ExpiresIn { get; set; }
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
    public required string MkanId { get; set; }
    
    /// <summary>
    /// Organizational hierarchy
    /// </summary>
    public required OrganizationHierarchyDto Hierarchy { get; set; }

    /// <summary>
    /// Roles assigned to member at various levels
    /// Format: "DepartmentName:LevelType" e.g., "Taleem:DepartmentOfficer"
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
    public int StateId { get; set; }
    public required string StateName { get; set; }
    public required string Country { get; set; } = "Nigeria"; // Always Nigeria
}

/// <summary>
/// Role information with department and level
/// </summary>
public class RoleDto
{
    public required string DepartmentName { get; set; }
    public required string LevelType { get; set; } // e.g., "DepartmentOfficer", "UnitPresident", "StateGS"
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
    public required string MkanId { get; set; }
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
