using AMSAReportingSystem.Data.Entities;
using AMSAReportingSystem.Core.Abstractions;

namespace AMSAReportingSystem.Services;

public class ReportAccessService : IReportAccessPolicy
{
    public bool IsNationalLeadership(CurrentUserScope actor) =>
        HasLeadershipAtLevel(actor, OrganizationLevel.National) || actor.HasSudoAccess;

    public bool IsStateLeadership(CurrentUserScope actor) =>
        HasLeadershipAtLevel(actor, OrganizationLevel.State) || actor.HasSudoAccess;

    public bool IsUnitLeadership(CurrentUserScope actor) =>
        HasLeadershipAtLevel(actor, OrganizationLevel.Unit) || actor.HasSudoAccess;

    public bool CanEditDepartment(CurrentUserScope actor, int unitId, int stateId, string departmentName)
    {
        if (IsNationalLeadership(actor))
        {
            return true;
        }

        if (stateId == actor.StateId && IsStateLeadership(actor))
        {
            return true;
        }

        if (unitId == actor.UnitId && IsUnitLeadership(actor))
        {
            return true;
        }

        if (stateId == actor.StateId && HasDepartmentAtLevel(actor, departmentName, OrganizationLevel.State))
        {
            return true;
        }

        if (unitId == actor.UnitId && HasDepartmentAtLevel(actor, departmentName, OrganizationLevel.Unit))
        {
            return true;
        }

        if (HasDepartmentAtLevel(actor, departmentName, OrganizationLevel.National))
        {
            return true;
        }

        return false;
    }

    public bool CanInitiateReportSubmission(CurrentUserScope actor, int unitId, int stateId)
    {
        if (IsNationalLeadership(actor))
        {
            return true;
        }

        if (stateId == actor.StateId && IsStateLeadership(actor))
        {
            return true;
        }

        if (unitId == actor.UnitId && IsUnitLeadership(actor))
        {
            return true;
        }

        if (unitId == actor.UnitId && stateId == actor.StateId)
        {
            return actor.Roles.Any(r => r.Level == OrganizationLevel.Unit);
        }

        return false;
    }

    public bool CanReviewAtUnitLevel(CurrentUserScope actor, int unitId) => IsNationalLeadership(actor) || (unitId == actor.UnitId && IsUnitLeadership(actor));

    public bool CanReviewAtStateLevel(CurrentUserScope actor, int stateId) => IsNationalLeadership(actor) || (stateId == actor.StateId && IsStateLeadership(actor));

    public bool IsNationalLeadership(AuthContext actor) =>
        HasLeadershipAtLevel(actor, LevelType.National) || actor.HasSudoAccess;

    public bool IsStateLeadership(AuthContext actor) =>
        HasLeadershipAtLevel(actor, LevelType.State) || actor.HasSudoAccess;

    public bool IsUnitLeadership(AuthContext actor) =>
        HasLeadershipAtLevel(actor, LevelType.Unit) || actor.HasSudoAccess;

    public bool CanEditDepartment(AuthContext actor, int unitId, int stateId, DepartmentType department)
    {
        return CanEditDepartment(ToScope(actor), unitId, stateId, department.ToString());
    }

    public bool CanInitiateReportSubmission(AuthContext actor, int unitId, int stateId)
    {
        return CanInitiateReportSubmission(ToScope(actor), unitId, stateId);
    }

    public bool CanReviewAtUnitLevel(AuthContext actor, int unitId) => IsNationalLeadership(actor) || (unitId == actor.UnitId && IsUnitLeadership(actor));

    public bool CanReviewAtStateLevel(AuthContext actor, int stateId) => IsNationalLeadership(actor) || (stateId == actor.StateId && IsStateLeadership(actor));

    private static CurrentUserScope ToScope(AuthContext actor) => new(
        actor.MemberId,
        actor.UnitId,
        actor.StateId,
        actor.NationalId,
        [.. actor.ParsedRoles.Select(r => new RoleScope(r.DepartmentName, ToOrganizationLevel(r.LevelType)))],
        actor.HasSudoAccess);

    private static OrganizationLevel ToOrganizationLevel(LevelType levelType) => levelType switch
    {
        LevelType.Unit => OrganizationLevel.Unit,
        LevelType.State => OrganizationLevel.State,
        LevelType.National => OrganizationLevel.National,
        _ => OrganizationLevel.Unit
    };

    private static bool HasLeadershipAtLevel(CurrentUserScope actor, OrganizationLevel levelType) =>
        actor.Roles.Any(r => r.Level == levelType && IsLeadershipDepartment(r.DepartmentName, levelType));

    private static bool HasDepartmentAtLevel(CurrentUserScope actor, string department, OrganizationLevel levelType)
    {
        var normalizedTarget = NormalizeDepartmentName(department);
        return actor.Roles.Any(r => r.Level == levelType
            && NormalizeDepartmentName(r.DepartmentName).Equals(normalizedTarget, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsLeadershipDepartment(string departmentName, OrganizationLevel levelType)
    {
        var normalized = NormalizeDepartmentName(departmentName);

        if (IsVpDepartment(normalized) && levelType != OrganizationLevel.National)
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

    private static bool HasLeadershipAtLevel(AuthContext actor, LevelType levelType) =>
        actor.ParsedRoles.Any(r => r.LevelType == levelType && IsLeadershipDepartment(r.DepartmentName, r.LevelType));

    private static bool HasDepartmentAtLevel(AuthContext actor, string department, LevelType levelType)
    {
        var normalizedTarget = NormalizeDepartmentName(department);
        return actor.ParsedRoles.Any(r => r.LevelType == levelType
            && NormalizeDepartmentName(r.DepartmentName).Equals(normalizedTarget, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsLeadershipDepartment(string departmentName, LevelType levelType)
    {
        var normalized = NormalizeDepartmentName(departmentName);

        // VP* leadership roles are valid only at National level.
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

    private static string NormalizeDepartmentName(string name)
    {
        var normalized = name.Trim().ToLowerInvariant()
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace("/", string.Empty);

        return normalized switch
        {
            "secondaryschool" => "secondaryschool",
            "generalsecretary" => "general",
            "assistantfinance" => "finance",
            "assistantgeneral" => "general",
            "assistanthealth" => "health",
            "assistantpublicity" => "publicity",
            "assistantsecondaryschool" => "secondaryschool",
            "assistantsport" => "sport",
            "assistanttabligh" => "tabligh",
            "assistanttajneed" => "tajneed",
            "assistanttaleem" => "taleem",
            "assistantwelfare" => "welfare",
            _ => normalized
        };
    }

    private static bool IsVpDepartment(string normalizedDepartment) =>
        normalizedDepartment is "vicepresident" or "vpadmin" or "vpnorth" or "vpsouthsouthsoutheast" or "vpsouthwest";
}
