namespace AMSAReportingSystem.Core.Abstractions;

public interface IReportAccessPolicy
{
    bool IsNationalLeadership(CurrentUserScope actor);
    bool IsStateLeadership(CurrentUserScope actor);
    bool IsUnitLeadership(CurrentUserScope actor);
    bool CanInitiateReportSubmission(CurrentUserScope actor, int unitId, int stateId);
    bool CanReviewAtUnitLevel(CurrentUserScope actor, int unitId);
    bool CanReviewAtStateLevel(CurrentUserScope actor, int stateId);
    bool CanEditDepartment(CurrentUserScope actor, int unitId, int stateId, string departmentName);
}