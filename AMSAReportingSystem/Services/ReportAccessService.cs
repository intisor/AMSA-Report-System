using AMSAReportingSystem.Data.Entities;

namespace AMSAReportingSystem.Services;

public class ReportAccessService
{
    public bool IsNationalLeadership(AuthContext actor) =>
        HasLeadershipAtLevel(actor, "National") || actor.HasSudoAccess;

    public bool IsStateLeadership(AuthContext actor) =>
        HasLeadershipAtLevel(actor, "State") || actor.HasSudoAccess;

    public bool IsUnitLeadership(AuthContext actor) =>
        HasLeadershipAtLevel(actor, "Unit") || actor.HasSudoAccess;

    public bool CanEditDepartment(AuthContext actor, int unitId, int stateId, DepartmentType department)
    {
        var departmentName = department.ToString();

        // National leadership can edit all units/departments.
        if (IsNationalLeadership(actor))
        {
            return true;
        }

        // State leadership can edit all units/departments in their state.
        if (stateId == actor.StateId && IsStateLeadership(actor))
        {
            return true;
        }

        // Unit leadership can edit all departments in their unit.
        if (unitId == actor.UnitId && IsUnitLeadership(actor))
        {
            return true;
        }

        // State office holders for same department across units in their state.
        if (stateId == actor.StateId && HasDepartmentAtLevel(actor, departmentName, "State"))
        {
            return true;
        }

        // Unit office holders for same department inside own unit.
        if (unitId == actor.UnitId && HasDepartmentAtLevel(actor, departmentName, "Unit"))
        {
            return true;
        }

        // National office holders for same department across all states.
        if (HasDepartmentAtLevel(actor, departmentName, "National"))
        {
            return true;
        }

        return false;
    }

    public bool CanSubmitReportToPresident(AuthContext actor, int unitId, int stateId)
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

        // Unit-level officers can submit for their own unit.
        return unitId == actor.UnitId && actor.ParsedRoles.Any(r => r.LevelType.Equals("Unit", StringComparison.OrdinalIgnoreCase));
    }

    public bool CanReviewAtUnitLevel(AuthContext actor, int unitId) =>
        IsNationalLeadership(actor) || (unitId == actor.UnitId && IsUnitLeadership(actor));

    public bool CanReviewAtStateLevel(AuthContext actor, int stateId) =>
        IsNationalLeadership(actor) || (stateId == actor.StateId && IsStateLeadership(actor));

    private static bool HasLeadershipAtLevel(AuthContext actor, string levelType) =>
        actor.ParsedRoles.Any(r =>
            r.LevelType.Equals(levelType, StringComparison.OrdinalIgnoreCase)
            && IsLeadershipDepartment(r.DepartmentName));

    private static bool HasDepartmentAtLevel(AuthContext actor, string department, string levelType)
    {
        var normalizedTarget = NormalizeDepartmentName(department);
        return actor.ParsedRoles.Any(r =>
            r.LevelType.Equals(levelType, StringComparison.OrdinalIgnoreCase)
            && NormalizeDepartmentName(r.DepartmentName).Equals(normalizedTarget, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsLeadershipDepartment(string departmentName)
    {
        var normalized = NormalizeDepartmentName(departmentName);
        return normalized is "president" or "general";
    }

    private static string NormalizeDepartmentName(string name)
    {
        var normalized = name.Trim().ToLowerInvariant()
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace("/", string.Empty);

        if (normalized.StartsWith("assistant"))
        {
            normalized = normalized["assistant".Length..];
        }

        return normalized switch
        {
            "secondaryschool" => "secondaryschool",
            "generalsecretary" => "general",
            _ => normalized
        };
    }
}
