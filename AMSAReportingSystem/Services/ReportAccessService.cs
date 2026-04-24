using AMSAReportingSystem.Data.Entities;

namespace AMSAReportingSystem.Services;

public class ReportAccessService
{
    public bool IsNationalLeadership(AuthContext actor) =>
        HasAnyLevel(actor, "NationalPresident", "NationalGS", "AssistantGS", "NationalAssistantGS");

    public bool IsStateLeadership(AuthContext actor) =>
        HasAnyLevel(actor, "StatePresident", "StateGS", "StateGeneralSecretary");

    public bool IsUnitLeadership(AuthContext actor) =>
        HasAnyLevel(actor, "UnitPresident", "UnitGS", "UnitGeneralSecretary");

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

        // Department officers: same office only, with scope by level.
        if (HasDepartmentRole(actor, departmentName, "DepartmentOfficer"))
        {
            // Unit office can edit only inside own unit.
            if (unitId == actor.UnitId)
            {
                return true;
            }
        }

        // State office holders for same department across units in their state.
        if (stateId == actor.StateId && HasDepartmentAnyLevel(actor, departmentName, "State"))
        {
            return true;
        }

        // National office holders for same department across all states.
        if (HasDepartmentAnyLevel(actor, departmentName, "National"))
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

        // Unit officers can submit for their own unit.
        return unitId == actor.UnitId && actor.Roles.Any(r => r.EndsWith(":DepartmentOfficer", StringComparison.OrdinalIgnoreCase));
    }

    public bool CanReviewAtUnitLevel(AuthContext actor, int unitId) =>
        IsNationalLeadership(actor) || (unitId == actor.UnitId && IsUnitLeadership(actor));

    public bool CanReviewAtStateLevel(AuthContext actor, int stateId) =>
        IsNationalLeadership(actor) || (stateId == actor.StateId && IsStateLeadership(actor));

    private static bool HasAnyLevel(AuthContext actor, params string[] levelTypes)
    {
        return actor.Roles.Any(role =>
        {
            var split = role.Split(':', 2);
            if (split.Length != 2) return false;
            var level = split[1];
            return levelTypes.Any(expected => level.Equals(expected, StringComparison.OrdinalIgnoreCase));
        });
    }

    private static bool HasDepartmentRole(AuthContext actor, string department, string level)
    {
        return actor.Roles.Any(role =>
        {
            var split = role.Split(':', 2);
            if (split.Length != 2) return false;
            return split[0].Equals(department, StringComparison.OrdinalIgnoreCase)
                   && split[1].Equals(level, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static bool HasDepartmentAnyLevel(AuthContext actor, string department, string levelContains)
    {
        return actor.Roles.Any(role =>
        {
            var split = role.Split(':', 2);
            if (split.Length != 2) return false;
            return split[0].Equals(department, StringComparison.OrdinalIgnoreCase)
                   && split[1].Contains(levelContains, StringComparison.OrdinalIgnoreCase);
        });
    }
}
