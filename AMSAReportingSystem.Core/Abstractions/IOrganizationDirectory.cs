namespace AMSAReportingSystem.Core.Abstractions;

public interface IOrganizationDirectory
{
    ValueTask<string> GetStateNameAsync(int stateId, CancellationToken cancellationToken = default);
    ValueTask<string> GetUnitNameAsync(int unitId, CancellationToken cancellationToken = default);
}
