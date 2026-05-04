using AMSAReportingSystem.Data.Entities;

namespace AMSAReportingSystem.Services;

/// <summary>
/// Convenience service for accessing report operations as the currently authenticated user
/// Wraps UnifiedReportService with automatic user context retrieval
/// Reduces boilerplate in components and reduces need to pass auth context everywhere
/// </summary>
public class CurrentUserReportService
{
    private readonly AMSAAuthStateProvider _authStateProvider;
    private readonly UnifiedReportService _reportService;

    public CurrentUserReportService(
        AMSAAuthStateProvider authStateProvider,
        UnifiedReportService reportService)
    {
        _authStateProvider = authStateProvider;
        _reportService = reportService;
    }

    #region Reporting Cycle & Draft Management

    public Task<ReportingCycle> EnsureActiveCycleAsync(CancellationToken ct = default) =>
        _reportService.EnsureActiveCycleAsync(ct);

    public Task<Report> EnsureCurrentUserDraftAsync(int cycleId, CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor => _reportService.EnsureDraftAsync(actor, actor.UnitId, cycleId, ct));

    #endregion

    #region Department Report Operations

    public async Task<DepartmentReport?> GetDepartmentAsync(int reportId, DepartmentType department, CancellationToken ct = default)
    {
        var report = await _reportService.GetReportAsync(reportId, ct);
        return report?.DepartmentReports.FirstOrDefault(d => d.Department == department);
    }

    public Task<DepartmentReport> SaveDepartmentJsonAsync(
        int reportId,
        DepartmentType department,
        string reportDataJson,
        bool markSubmitted,
        CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor =>
            _reportService.SaveDepartmentDataAsync(reportId, department, reportDataJson, actor, markSubmitted, ct));

    #endregion

    #region Unit-Level Authorization & Actions

    public bool CanEditOwnUnitDepartment(DepartmentType department)
    {
        var user = _authStateProvider.GetCurrentUser();
        if (user is null || !user.IsAuthenticated)
            return false;

        var access = new ReportAccessService();
        return access.CanEditDepartment(user, user.UnitId, user.StateId, department);
    }

    public bool CanInitiateCurrentUserReportSubmission()
    {
        var user = _authStateProvider.GetCurrentUser();
        if (user is null || !user.IsAuthenticated)
            return false;

        var access = new ReportAccessService();
        return access.CanInitiateReportSubmission(user, user.UnitId, user.StateId);
    }

    public Task<Report> SubmitReportToPresidentAsync(int reportId, string? notes = null, CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor => _reportService.SubmitReportToPresidentAsync(actor, reportId, notes, ct));

    #endregion

    #region Unit/State/National Report Views & Leadership Actions

    public Task<List<Report>> GetMyUnitReportsAsync(int? cycleId = null, CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor => _reportService.GetUnitReportsAsync(actor, actor.UnitId, cycleId, ct));

    public Task<List<Report>> GetMyStateReportsAsync(int? cycleId = null, CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor => _reportService.GetStateReportsAsync(actor, actor.StateId, cycleId, ct));

    public Task<StateReport> EnsureMyStateReportAsync(int cycleId, CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor => _reportService.EnsureStateReportAsync(actor, actor.StateId, cycleId, ct));

    public Task<StateReport> SaveMyStateReportAsync(int stateReportId, StateReportForm form, bool markSubmitted, CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor => _reportService.SaveStateReportAsync(actor, stateReportId, form, markSubmitted, ct));

    public Task<List<Report>> GetNationalReportsAsync(int? cycleId = null, CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor => _reportService.GetNationalReportsAsync(actor, cycleId, ct));

    #endregion

    #region Leadership Approval & Rejection

    public Task<Report> ApproveAtUnitAsync(int reportId, string? notes = null, CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor => _reportService.ApproveByUnitLeadershipAsync(actor, reportId, notes, ct));

    public Task<Report> RejectAtUnitAsync(int reportId, string? notes = null, CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor => _reportService.RejectByUnitLeadershipAsync(actor, reportId, notes, ct));

    public Task<Report> ApproveAtStateAsync(int reportId, string? notes = null, CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor => _reportService.ApproveByStateLeadershipAsync(actor, reportId, notes, ct));

    public Task<Report> RejectAtStateAsync(int reportId, string? notes = null, CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor => _reportService.RejectByStateLeadershipAsync(actor, reportId, notes, ct));

    public Task<Report> AcknowledgeAtNationalAsync(int reportId, string? notes = null, CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor => _reportService.AcknowledgeByNationalAsync(actor, reportId, notes, ct));

    #endregion

    #region Private Helpers

    private AuthContext GetCurrentUserOrThrow()
    {
        var user = _authStateProvider.GetCurrentUser();
        if (user is null || !user.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Login required.");
        }

        return user;
    }

    private Task<T> ExecuteAsCurrentUserAsync<T>(Func<AuthContext, Task<T>> operation) =>
        operation(GetCurrentUserOrThrow());

    private T ExecuteAsCurrentUser<T>(Func<AuthContext, T> operation) =>
        operation(GetCurrentUserOrThrow());

    #endregion
}
