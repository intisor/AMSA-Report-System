using AMSAReportingSystem.Data.Entities;

namespace AMSAReportingSystem.Services;

public class ReportAccessService
{
    public bool IsNationalLeadership(AuthContext actor) =>
        HasLeadershipAtLevel(actor, LevelType.National) || actor.HasSudoAccess;

    public bool IsStateLeadership(AuthContext actor) =>
        HasLeadershipAtLevel(actor, LevelType.State) || actor.HasSudoAccess;

    public bool IsUnitLeadership(AuthContext actor) =>
        HasLeadershipAtLevel(actor, LevelType.Unit) || actor.HasSudoAccess;

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

        // State office holders for same department across units in their state can edit too
        if (stateId == actor.StateId && HasDepartmentAtLevel(actor, departmentName, LevelType.State))
        {
            return true;
        }

        // Unit office holders for same department inside own unit can edut 
        if (unitId == actor.UnitId && HasDepartmentAtLevel(actor, departmentName, LevelType.Unit))
        {
            return true;
        }

        // National office holders for same department across all states.
        if (HasDepartmentAtLevel(actor, departmentName, LevelType.National))
        {
            return true;
        }

        return false;
    }

    public bool CanInitiateReportSubmission(AuthContext actor, int unitId, int stateId)
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

        // Department officers in the same unit/state can create/manage draft report shell.
        if (unitId == actor.UnitId && stateId == actor.StateId)
        {
            return actor.ParsedRoles.Any(r => r.LevelType == LevelType.Unit);
        }

        return false;
    }

    public bool CanReviewAtUnitLevel(AuthContext actor, int unitId) => IsNationalLeadership(actor) || (unitId == actor.UnitId && IsUnitLeadership(actor));

    public bool CanReviewAtStateLevel(AuthContext actor, int stateId) => IsNationalLeadership(actor) || (stateId == actor.StateId && IsStateLeadership(actor));

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
