using AMSAReportingSystem.Data.Entities;

namespace AMSAReportingSystem.Services;

public class CurrentUserReportService
{
    private readonly AmsaAuthStateProvider _authStateProvider;
    private readonly ReportAccessService _reportAccessService;
    private readonly ReportService _reportService;

    public CurrentUserReportService(
        AmsaAuthStateProvider authStateProvider,
        ReportAccessService reportAccessService,
        ReportService reportService)
    {
        _authStateProvider = authStateProvider;
        _reportAccessService = reportAccessService;
        _reportService = reportService;
    }

    public async Task<Report> GetOrCreateCurrentUserDraftAsync(int cycleId, CancellationToken ct = default)
    {
        var actor = GetCurrentUserOrThrow();
        return await _reportService.GetOrCreateDraftAsync(actor, actor.UnitId, cycleId, ct);
    }

    public async Task<Report> GetOrCreateCurrentUserDraftAsync(CancellationToken ct = default)
    {
        var cycle = await _reportService.GetOrCreateActiveCycleAsync(ct);
        return await GetOrCreateCurrentUserDraftAsync(cycle.Id, ct);
    }

    public Task<ReportingCycle> GetActiveCycleAsync(CancellationToken ct = default) =>
        _reportService.GetOrCreateActiveCycleAsync(ct);

    public async Task<DepartmentReport?> GetDepartmentAsync(int reportId, DepartmentType department, CancellationToken ct = default)
    {
        var report = await _reportService.GetReportAsync(reportId, ct);
        return report?.DepartmentReports.FirstOrDefault(d => d.Department == department);
    }

    public async Task<DepartmentReport> SaveDepartmentJsonAsync(
        int reportId,
        DepartmentType department,
        string reportDataJson,
        bool markSubmitted,
        CancellationToken ct = default)
    {
        var actor = GetCurrentUserOrThrow();
        return await _reportService.SaveDepartmentDataAsync(reportId, department, reportDataJson, actor, markSubmitted, ct);
    }

    public bool CanEditOwnUnitDepartment(DepartmentType department)
    {
        var actor = GetCurrentUserOrThrow();
        return _reportAccessService.CanEditDepartment(actor, actor.UnitId, actor.StateId, department);
    }

    public async Task<Report> SubmitReportToPresidentAsync(int reportId, string? notes = null, CancellationToken ct = default)
    {
        var actor = GetCurrentUserOrThrow();
        return await _reportService.SubmitReportToPresidentAsync(actor, reportId, notes, ct);
    }

    public async Task<List<Report>> GetMyUnitReportsAsync(int? cycleId = null, CancellationToken ct = default)
    {
        var actor = GetCurrentUserOrThrow();
        return await _reportService.GetUnitReportsAsync(actor, actor.UnitId, cycleId, ct);
    }

    public async Task<List<Report>> GetMyStateReportsAsync(int? cycleId = null, CancellationToken ct = default)
    {
        var actor = GetCurrentUserOrThrow();
        return await _reportService.GetStateReportsAsync(actor, actor.StateId, cycleId, ct);
    }

    public async Task<List<Report>> GetNationalReportsAsync(int? cycleId = null, CancellationToken ct = default)
    {
        var actor = GetCurrentUserOrThrow();
        return await _reportService.GetNationalReportsAsync(actor, cycleId, ct);
    }

    public async Task<Report> ApproveAtUnitAsync(int reportId, string? notes = null, CancellationToken ct = default)
    {
        var actor = GetCurrentUserOrThrow();
        return await _reportService.ApproveByUnitLeadershipAsync(actor, reportId, notes, ct);
    }

    public async Task<Report> RejectAtUnitAsync(int reportId, string? notes = null, CancellationToken ct = default)
    {
        var actor = GetCurrentUserOrThrow();
        return await _reportService.RejectByUnitLeadershipAsync(actor, reportId, notes, ct);
    }

    public async Task<Report> ApproveAtStateAsync(int reportId, string? notes = null, CancellationToken ct = default)
    {
        var actor = GetCurrentUserOrThrow();
        return await _reportService.ApproveByStateLeadershipAsync(actor, reportId, notes, ct);
    }

    public async Task<Report> RejectAtStateAsync(int reportId, string? notes = null, CancellationToken ct = default)
    {
        var actor = GetCurrentUserOrThrow();
        return await _reportService.RejectByStateLeadershipAsync(actor, reportId, notes, ct);
    }

    public async Task<Report> AcknowledgeAtNationalAsync(int reportId, string? notes = null, CancellationToken ct = default)
    {
        var actor = GetCurrentUserOrThrow();
        return await _reportService.AcknowledgeByNationalAsync(actor, reportId, notes, ct);
    }

    private AuthContext GetCurrentUserOrThrow()
    {
        var user = _authStateProvider.GetCurrentUser();
        if (user is null || !user.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Login required.");
        }

        return user;
    }
}
