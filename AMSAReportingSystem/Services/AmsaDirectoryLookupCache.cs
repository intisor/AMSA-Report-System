namespace AMSAReportingSystem.Services;

/// <summary>
/// Scoped per-circuit cache for AMSA state/unit display names.
/// </summary>
public class AmsaDirectoryLookupCache(IAmsaApiClient amsaApiClient)
{
    private readonly IAmsaApiClient _amsaApiClient = amsaApiClient;
    private readonly Dictionary<int, string> _stateNames = [];
    private readonly Dictionary<int, string> _unitNames = [];

    public async Task<string> GetStateNameAsync(int stateId, CancellationToken ct = default)
    {
        if (_stateNames.TryGetValue(stateId, out var cached))
        {
            return cached;
        }

        var stateResult = await _amsaApiClient.GetStateByIdAsync(stateId, ct);
        var name = stateResult.IsSuccess && stateResult.Data is not null
            ? stateResult.Data.StateName
            : $"State {stateId}";
        _stateNames[stateId] = name;
        return name;
    }

    public async Task<string> GetUnitNameAsync(int unitId, CancellationToken ct = default)
    {
        if (_unitNames.TryGetValue(unitId, out var cached))
        {
            return cached;
        }

        var unitResult = await _amsaApiClient.GetUnitByIdAsync(unitId, ct);
        var name = unitResult.IsSuccess && unitResult.Data is not null
            ? unitResult.Data.UnitName
            : $"Unit {unitId}";
        _unitNames[unitId] = name;
        return name;
    }
}
