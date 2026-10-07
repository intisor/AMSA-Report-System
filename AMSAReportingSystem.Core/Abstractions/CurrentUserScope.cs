namespace AMSAReportingSystem.Core.Abstractions;

public enum OrganizationLevel
{
    Unit,
    State,
    National
}

public sealed record RoleScope(string DepartmentName, OrganizationLevel Level);

public sealed record CurrentUserScope(
    int MemberId,
    int UnitId,
    int StateId,
    int NationalId,
    IReadOnlyList<RoleScope> Roles,
    bool HasSudoAccess);