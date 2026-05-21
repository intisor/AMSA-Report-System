using AMSAReportingSystem.Data;
using AMSAReportingSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AMSAReportingSystem.Services;

#region UnifiedReportService

/// <summary>
/// Unified service for all report-related operations
/// Consolidates DepartmentReportService, StateReportService, and ReportLifecycleService functionality
/// Organized by operational scope: Department Ops, State Ops, Report Lifecycle, and Cycles
/// </summary>
public class UnifiedReportService(AMSAReportingDbContext db,IAmsaApiClient amSaApiClient,ReportAccessService access,ILogger<UnifiedReportService> logger)
{
    private const int DefaultSubmissionGraceDays = 7;

    private readonly AMSAReportingDbContext _db = db;
    private readonly IAmsaApiClient _amSaApiClient = amSaApiClient;
    private readonly ReportAccessService _access = access;
    private readonly ILogger<UnifiedReportService> _logger = logger;

    #region Department Report Operations

    /// <summary>
    /// Saves department-specific report data to database
    /// Validates JSON, checks permissions, and updates submission status
    /// </summary>
    public async Task<DepartmentReport> SaveDepartmentDataAsync(int reportId,DepartmentType department,string reportDataJson,AuthContext actor,bool markSubmitted,CancellationToken ct = default)
    {
        ValidateJson(reportDataJson);

        var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == reportId, ct)
            ?? throw new InvalidOperationException($"Report {reportId} not found.");

        if (!_access.CanEditDepartment(actor, report.UnitId, report.StateId, department))
            throw new UnauthorizedAccessException("You are not allowed to edit this department report.");
        

        var departmentReport = await _db.DepartmentReports
            .FirstOrDefaultAsync(d => d.ReportId == reportId && d.Department == department, ct);

        if (departmentReport is null)
        {
            departmentReport = new DepartmentReport
            {
                ReportId = reportId,
                CycleId = report.CycleId,
                Department = department,
                ReportData = reportDataJson,
                IsSubmitted = markSubmitted,
                IsCompliant = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.DepartmentReports.Add(departmentReport);
        }
        else
        {
            departmentReport.ReportData = reportDataJson;
            if (markSubmitted)
            {
                departmentReport.IsSubmitted = true;
                departmentReport.SubmittedAt = DateTime.UtcNow;
                departmentReport.SubmittedByMemberId = actor.MemberId;
            }
            departmentReport.UpdatedAt = DateTime.UtcNow;
        }

        _db.ReportActivityLogs.Add(new ReportActivityLog
        {
            ReportId = reportId,
            ActionByMemberId = actor.MemberId,
            Action = markSubmitted ? "DepartmentReportSubmitted" : "DepartmentReportSaved",
            ActionAt = DateTime.UtcNow,
            Notes = $"Updated {department} report"
        });

        await _db.SaveChangesAsync(ct);
        return departmentReport;
    }

    /// <summary>
    /// Retrieves a specific department report with full data
    /// </summary>
    public async Task<DepartmentReport?> GetDepartmentReportAsync(int reportId,DepartmentType department,CancellationToken ct = default)
    {
        return await _db.DepartmentReports
            .FirstOrDefaultAsync(d => d.ReportId == reportId && d.Department == department, ct);
    }

    #endregion

    #region State Report Operations

    /// <summary>
    /// Retrieves state-level report with authorization check
    /// Includes programs and activity logs
    /// </summary>
    public async Task<StateReport?> GetStateReportAsync(AuthContext actor,int stateId,int cycleId,CancellationToken ct = default)
    {
        if (!_access.CanReviewAtStateLevel(actor, stateId)) 
            throw new UnauthorizedAccessException("You are not allowed to manage this state report.");
        

        var stateReport = await _db.StateReports
            .Include(sr => sr.Programs)
            .FirstOrDefaultAsync(sr => sr.StateId == stateId && sr.CycleId == cycleId, ct);

        if (stateReport is null)
            return null;
        

        await RecomputeStateAggregateAsync(stateReport, ct);
        return stateReport;
    }

    /// <summary>
    /// Ensures a state report exists, creating if necessary
    /// Initializes with Draft status and links available submitted department reports
    /// </summary>
    public async Task<StateReport> EnsureStateReportAsync(AuthContext actor,int stateId,int cycleId,CancellationToken ct = default)
    {
        var existing = await GetStateReportAsync(actor, stateId, cycleId, ct);

        if (existing is not null)
            return existing;

        var report = new StateReport
        {
            StateId = stateId,
            CycleId = cycleId,
            Status = ReportStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.StateReports.Add(report);
        await _db.SaveChangesAsync(ct);

        await RecomputeStateAggregateAsync(report, ct);

        return await _db.StateReports
            .Include(sr => sr.Programs)
            .FirstAsync(sr => sr.Id == report.Id, ct);
    }

    /// <summary>
    /// Updates state report with aggregated data and leadership notes
    /// Validates form data and manages program list updates
    /// </summary>
    public async Task<StateReport> SaveStateReportAsync(AuthContext actor,int stateReportId,StateReportForm form,bool markSubmitted,CancellationToken ct = default)
    {
        var stateReport = await _db.StateReports
            .Include(sr => sr.Programs)
            .FirstOrDefaultAsync(sr => sr.Id == stateReportId, ct)
            ?? throw new InvalidOperationException($"State report {stateReportId} not found.");

        if (!_access.CanReviewAtStateLevel(actor, stateReport.StateId))
            throw new UnauthorizedAccessException("You are not allowed to edit this state report.");

        await EnsureCycleOpenForEditsAsync(stateReport.CycleId, ct);

        if (stateReport.Status is ReportStatus.SubmittedToNational or ReportStatus.Acknowledged)
            throw new InvalidOperationException("This state report has already been submitted and is read-only.");
        

        ValidateStateReportForm(form);

        // Preserve leadership commentary fields, but enforce aggregate metrics/programs from unit reports.
        stateReport.UnitImprovementPlan = form.UnitImprovementPlan;
        stateReport.ChallengesFaced = form.ChallengesFaced;
        stateReport.NationalSupportNeeded = form.NationalSupportNeeded;
        stateReport.OtherNotes = form.OtherNotes;
        stateReport.UpdatedAt = DateTime.UtcNow;

        await RecomputeStateAggregateAsync(stateReport, ct);

        if (markSubmitted)
        {
            stateReport.Status = ReportStatus.SubmittedToNational;
            stateReport.SubmittedAt = DateTime.UtcNow;
            stateReport.SubmittedByMemberId = actor.MemberId;
        }

        await _db.SaveChangesAsync(ct);

        return await _db.StateReports
            .Include(sr => sr.Programs)
            .FirstAsync(sr => sr.Id == stateReport.Id, ct);
    }

    #endregion

    #region Report Lifecycle & Submission Operations

    /// <summary>
    /// Retrieves a specific report with all related data
    /// </summary>
    public async Task<Report?> GetReportAsync(int reportId, CancellationToken ct = default)
    {
        return await _db.Reports
            .Include(r => r.Cycle)
            .Include(r => r.DepartmentReports)
            .Include(r => r.ActivityLogs)
            .FirstOrDefaultAsync(r => r.Id == reportId, ct);
    }

    /// <summary>
    /// Retrieves draft report for unit if authorized
    /// </summary>
    public async Task<Report?> GetDraftAsync(AuthContext actor, int amsaUnitId, int cycleId, CancellationToken ct = default)
    {
        if (!_access.CanInitiateReportSubmission(actor, amsaUnitId, actor.StateId))
            throw new UnauthorizedAccessException("You are not allowed to create or manage reports for this unit.");
        

        var currentUser = actor;

        var  reports =  await _db.Reports
            .Include(r => r.DepartmentReports)
            .Include(r => r.ActivityLogs)
            .FirstOrDefaultAsync(r => r.UnitId == amsaUnitId && r.CycleId == cycleId, ct);

        return reports ?? null;
    }

    /// <summary>
    /// Creates draft report for unit with all departments initialized
    /// </summary>
    public async Task<Report> EnsureDraftAsync(AuthContext actor, int amsaUnitId, int cycleId, CancellationToken ct = default)
    {
        var existing = await GetDraftAsync(actor, amsaUnitId, cycleId, ct);
        if (existing is not null)
            return existing;
        

        var report = new Report
        {
            UnitId = amsaUnitId,
            StateId = actor.StateId,
            CycleId = cycleId,
            Status = ReportStatus.Draft,
            IsCompliant = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Reports.Add(report);
        await _db.SaveChangesAsync(ct);

        foreach (var department in ReportDepartmentCatalog.ReportableDepartments)
        {
            _db.DepartmentReports.Add(new DepartmentReport
            {
                ReportId = report.Id,
                CycleId = report.CycleId,
                Department = department,
                ReportData = "{}",
                IsSubmitted = false,
                IsCompliant = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        _db.ReportActivityLogs.Add(new ReportActivityLog
        {
            ReportId = report.Id,
            ActionByMemberId = actor.MemberId,
            Action = "ReportCreated",
            ActionAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        return await GetReportAsync(report.Id, ct) ?? report;
    }

    /// <summary>
    /// Retrieves all reports for a unit with optional cycle filter
    /// </summary>
    public async Task<List<Report>> GetUnitReportsAsync(AuthContext actor, int unitId, int? cycleId = null, CancellationToken ct = default)
    {
        if (!_access.CanReviewAtUnitLevel(actor, unitId))
            throw new UnauthorizedAccessException("You are not allowed to view reports for this unit.");
        
        var selectedCycleId = await ResolveCycleIdAsync(cycleId, ct);
        if (selectedCycleId is null)
            return [];
  

        return await _db.Reports
            .Include(r => r.Cycle)
            .Include(r => r.DepartmentReports)
            .Where(r => r.UnitId == unitId && r.CycleId == selectedCycleId.Value)
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Retrieves all reports for a unit across all cycles for read-only retrieval
    /// </summary>
    public async Task<List<Report>> GetUnitReportHistoryAsync(AuthContext actor, int unitId, CancellationToken ct = default)
    {
        //if (!_access.CanReviewAtUnitLevel(actor, unitId))
        //    throw new UnauthorizedAccessException("You are not allowed to view reports for this unit.");

        return await _db.Reports
            .Include(r => r.Cycle)
            .Include(r => r.DepartmentReports)
            .Include(r => r.ActivityLogs)
            .Where(r => r.UnitId == unitId)
            .OrderByDescending(r => r.Cycle.StartDate)
            .ThenByDescending(r => r.UpdatedAt)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Retrieves all reports for a state with optional cycle filter
    /// </summary>
    public async Task<List<Report>> GetStateReportsAsync(AuthContext actor, int stateId, int? cycleId = null, CancellationToken ct = default)
    {
        if (!_access.CanReviewAtStateLevel(actor, stateId))
            throw new UnauthorizedAccessException("You are not allowed to view reports for this state.");
        

        var selectedCycleId = await ResolveCycleIdAsync(cycleId, ct);
        if (selectedCycleId is null)
            return [];

        return await _db.Reports
            .Include(r => r.Cycle)
            .Include(r => r.DepartmentReports)
            .Where(r => r.CycleId == selectedCycleId.Value && r.StateId == stateId)
            .OrderBy(r => r.UpdatedAt)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Retrieves all reports nationally with optional cycle filter
    /// National leadership only
    /// </summary>
    public async Task<List<Report>> GetNationalReportsAsync(AuthContext actor, int? cycleId = null, CancellationToken ct = default)
    {
        if (!_access.IsNationalLeadership(actor))
            throw new UnauthorizedAccessException("Only national leadership can view national report board.");
        

        var selectedCycleId = await ResolveCycleIdAsync(cycleId, ct);
        if (selectedCycleId is null)
            return [];
        

        return await _db.Reports
            .Include(r => r.Cycle)
            .Include(r => r.DepartmentReports)
            .Where(r => r.CycleId == selectedCycleId.Value)
            .OrderBy(r => r.StateId)
            .ThenBy(r => r.UnitId)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Retrieves all reports nationally WITH state leadership context
    /// Combines Report + StateReport so National sees state's commentary, challenges, plans
    /// National leadership only
    /// </summary>
    public async Task<List<ReportWithStateContext>> GetNationalReportsWithStateContextAsync(AuthContext actor, int? cycleId = null, CancellationToken ct = default)
    {
        if (!_access.IsNationalLeadership(actor))
            throw new UnauthorizedAccessException("Only national leadership can view national report board.");

        var selectedCycleId = await ResolveCycleIdAsync(cycleId, ct);
        if (selectedCycleId is null)
            return [];

        var reports = await _db.Reports
            .Include(r => r.Cycle)
            .Include(r => r.DepartmentReports)
            .Include(r => r.ActivityLogs)
            .Where(r => r.CycleId == selectedCycleId.Value)
            .OrderBy(r => r.StateId)
            .ThenBy(r => r.UnitId)
            .AsNoTracking()
            .ToListAsync(ct);

        var result = new List<ReportWithStateContext>();

        foreach (var report in reports)
        {
            // Get state report context for this state+cycle
            var stateReport = await _db.StateReports
                .Include(sr => sr.Programs)
                .AsNoTracking()
                .FirstOrDefaultAsync(sr => sr.StateId == report.StateId && sr.CycleId == selectedCycleId.Value, ct);

            // Map to composite DTO
            var reportWithContext = new ReportWithStateContext(
                ReportId: report.Id,
                UnitId: report.UnitId,
                StateId: report.StateId,
                CycleLabel: report.Cycle?.CycleMonth ?? $"Cycle {report.CycleId}",
                Status: report.Status,
                CreatedAt: report.CreatedAt,
                UpdatedAt: report.UpdatedAt,
                SubmittedToPresidentAt: report.SubmittedToPresidentAt,
                ApprovedByPresidentAt: report.ApprovedByPresidentAt,
                PresidentialNotes: report.PresidentialNotes,
                ApprovedByStateAt: report.ApprovedByStateAt,
                StateNotes: report.StateNotes,
                AcknowledgedByNationalAt: report.AcknowledgedByNationalAt,
                NationalNotes: report.NationalNotes,
                
                // State context (nullable - state may not have created a state report yet)
                StateContext: stateReport is null ? null : new StateReportContext(
                    StateReportId: stateReport.Id,
                    CycleLabel: report.Cycle?.CycleMonth ?? $"Cycle {stateReport.CycleId}",
                    UnitsAttendedTo: stateReport.UnitsAttendedTo,
                    TotalUnitReportsCount: stateReport.TotalUnitReportsCount,
                    UnitPerformanceRating: stateReport.UnitPerformanceRating,
                    UnitPresidentAttendanceRate: stateReport.UnitPresidentAttendanceRate,
                    UnitImprovementPlan: stateReport.UnitImprovementPlan,
                    ChallengesFaced: stateReport.ChallengesFaced,
                    NationalSupportNeeded: stateReport.NationalSupportNeeded,
                    OtherNotes: stateReport.OtherNotes,
                    PresidentialNote: stateReport.PresidentialNote,
                    ApprovedByPresidentAt: stateReport.ApprovedByPresidentAt,
                    NationalNote: stateReport.NationalNote,
                    AcknowledgedByNationalAt: stateReport.AcknowledgedByNationalAt,
                    IsAggregated: stateReport.IsAggregated,
                    LastAggregatedAt: stateReport.LastAggregatedAt,
                    Programs: [.. stateReport.Programs
                        .Select(p => new StateReportProgramView(
                            p.ProgramId,
                            p.ProgramName,
                            p.Objectives,
                            p.Outcomes,
                            p.TotalAttendance,
                            p.TotalBeneficiaries,
                            p.IsAutoAggregated))]),
                
                // Department data
                Departments: [.. report.DepartmentReports
                    .OrderBy(d => d.Department)
                    .Select(d => new ReportDepartmentView(d.Department, d.Department.ToString(), d.IsSubmitted, d.SubmittedAt, d.ReportData))],
                
                // Activity logs
                ActivityLogs: [.. report.ActivityLogs
                    .OrderByDescending(log => log.ActionAt)
                    .Select(log => new ReportActivityView(log.ActionAt, log.Action, log.Notes))]);

            result.Add(reportWithContext);
        }

        return result;
    }

    /// <summary>
    /// Submits report from unit to president for review
    /// At least one departments must be submitted first
    /// </summary>
    public async Task<Report> SubmitReportToPresidentAsync(AuthContext actor, int reportId, string? notes = null, CancellationToken ct = default)
    {
        var report = await _db.Reports
            .Include(r => r.DepartmentReports)
            .FirstOrDefaultAsync(r => r.Id == reportId, ct) ?? throw new InvalidOperationException($"Report {reportId} not found.");

        await EnsureCycleOpenForEditsAsync(report.CycleId, ct);

        if (!_access.CanInitiateReportSubmission(actor, report.UnitId, report.StateId))
            throw new UnauthorizedAccessException("You are not allowed to submit this report.");
        

        if (report.Status != ReportStatus.Draft && report.Status != ReportStatus.RejectedByPresident)
            throw new InvalidOperationException($"Report {reportId} cannot be submitted from status {report.Status}.");
        

        if (!report.DepartmentReports.Any(d => d.IsSubmitted))
            throw new InvalidOperationException("At least one department report must be submitted before report submission.");
        

        report.Status = ReportStatus.SubmittedToPresident;
        report.SubmittedToPresidentAt = DateTime.UtcNow;
        report.SubmittedByMemberId = actor.MemberId;
        report.UpdatedAt = DateTime.UtcNow;

        _db.ReportActivityLogs.Add(new ReportActivityLog
        {
            ReportId = reportId,
            ActionByMemberId = actor.MemberId,
            Action = "ReportSubmittedToPresident",
            Notes = notes,
            ActionAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        return report;
    }

    /// <summary>
    /// Unit president approves report, moving to state level
    /// </summary>
    public async Task<Report> ApproveByUnitLeadershipAsync(AuthContext actor, int reportId, string? notes = null, CancellationToken ct = default)
    {
        var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == reportId, ct)
            ?? throw new InvalidOperationException($"Report {reportId} not found.");
        if (!_access.CanReviewAtUnitLevel(actor, report.UnitId))
            throw new UnauthorizedAccessException("You are not allowed to approve this report.");
        

        if (report.Status is not (ReportStatus.SubmittedToPresident or ReportStatus.Draft or ReportStatus.RejectedByPresident))
            throw new InvalidOperationException("Report cannot be approved from current state.");
        

        var submittedCount = await _db.DepartmentReports
            .Where(d => d.ReportId == report.Id && d.IsSubmitted)
            .CountAsync(ct);

        if (submittedCount == 0)
            throw new InvalidOperationException("At least one department section must be submitted before forwarding to state.");
        

        report.Status = ReportStatus.SubmittedToState;
        report.SubmittedToPresidentAt ??= DateTime.UtcNow;
        report.ApprovedByPresidentAt = DateTime.UtcNow;
        report.ApprovedByPresidentMemberId = actor.MemberId;
        report.PresidentialNotes = notes;
        report.UpdatedAt = DateTime.UtcNow;

        _db.ReportActivityLogs.Add(new ReportActivityLog
        {
            ReportId = report.Id,
            ActionByMemberId = actor.MemberId,
            Action = "ApprovedByUnitLeadership",
            Notes = notes,
            ActionAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        return report;
    }

    /// <summary>
    /// Unit president rejects report, sending back to unit for revisions
    /// </summary>
    public async Task<Report> RejectByUnitLeadershipAsync(AuthContext actor, int reportId, string? notes = null, CancellationToken ct = default)
    {
        var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == reportId, ct)
            ?? throw new InvalidOperationException($"Report {reportId} not found.");
        if (!_access.CanReviewAtUnitLevel(actor, report.UnitId))
            throw new UnauthorizedAccessException("You are not allowed to reject this report.");
        

        if (report.Status is not (ReportStatus.SubmittedToPresident or ReportStatus.Draft))
            throw new InvalidOperationException("Report cannot be rejected from current state.");

        report.Status = ReportStatus.RejectedByPresident;
        report.PresidentialNotes = notes;
        report.UpdatedAt = DateTime.UtcNow;

        _db.ReportActivityLogs.Add(new ReportActivityLog
        {
            ReportId = report.Id,
            ActionByMemberId = actor.MemberId,
            Action = "RejectedByUnitLeadership",
            Notes = notes,
            ActionAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        return report;
    }

    /// <summary>
    /// State leadership approves report, moving to national level
    /// </summary>
    public async Task<Report> ApproveByStateLeadershipAsync(AuthContext actor, int reportId, string? notes = null, CancellationToken ct = default)
    {
        var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == reportId, ct) ?? throw new InvalidOperationException($"Report {reportId} not found.");
        if (!_access.CanReviewAtStateLevel(actor, report.StateId))
            throw new UnauthorizedAccessException("You are not allowed to approve this report at state level.");
        
        if (report.Status != ReportStatus.SubmittedToState)
            throw new InvalidOperationException($"Report must be in {ReportStatus.SubmittedToState} state for state approval.");
        
        report.Status = ReportStatus.SubmittedToNational;
        report.ApprovedByStateAt = DateTime.UtcNow;
        report.ApprovedByStateMemberId = actor.MemberId;
        report.StateNotes = notes;
        report.UpdatedAt = DateTime.UtcNow;

        _db.ReportActivityLogs.Add(new ReportActivityLog
        {
            ReportId = report.Id,
            ActionByMemberId = actor.MemberId,
            Action = "ApprovedByStateLeadership",
            Notes = notes,
            ActionAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        return report;
    }

    /// <summary>
    /// State leadership rejects report, sending back to unit
    /// </summary>
    public async Task<Report> RejectByStateLeadershipAsync(AuthContext actor, int reportId, string? notes = null, CancellationToken ct = default)
    {
        var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == reportId, ct)  ?? throw new InvalidOperationException($"Report {reportId} not found.");

        if (!_access.CanReviewAtStateLevel(actor, report.StateId))
            throw new UnauthorizedAccessException("You are not allowed to reject this report at state level.");
        
        if (report.Status != ReportStatus.SubmittedToState)
            throw new InvalidOperationException($"Report must be in {ReportStatus.SubmittedToState} state for state rejection.");
        
        report.Status = ReportStatus.RejectedByState;
        report.StateNotes = notes;
        report.UpdatedAt = DateTime.UtcNow;

        _db.ReportActivityLogs.Add(new ReportActivityLog
        {
            ReportId = report.Id,
            ActionByMemberId = actor.MemberId,
            Action = "RejectedByStateLeadership",
            Notes = notes,
            ActionAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        return report;
    }

    /// <summary>
    /// National leadership acknowledges report completion
    /// </summary>
    public async Task<Report> AcknowledgeByNationalAsync(AuthContext actor, int reportId, string? notes = null, CancellationToken ct = default)
    {
        if (!_access.IsNationalLeadership(actor))
            throw new UnauthorizedAccessException("Only national leadership can acknowledge reports.");
        

        var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == reportId, ct) ?? throw new InvalidOperationException($"Report {reportId} not found.");

        if (report.Status != ReportStatus.SubmittedToNational)
            throw new InvalidOperationException($"Report must be in {ReportStatus.SubmittedToNational} state for national acknowledgment.");
        
        report.Status = ReportStatus.Acknowledged;
        report.AcknowledgedByNationalAt = DateTime.UtcNow;
        report.AcknowledgedByNationalMemberId = actor.MemberId;
        report.NationalNotes = notes;
        report.UpdatedAt = DateTime.UtcNow;

        _db.ReportActivityLogs.Add(new ReportActivityLog
        {
            ReportId = report.Id,
            ActionByMemberId = actor.MemberId,
            Action = "AcknowledgedByNational",
            Notes = notes,
            ActionAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        return report;
    }

    #endregion

    #region Reporting Cycle Management

    /// <summary>
    /// Retrieves the cycle for the current calendar month (deterministic, no grace days)
    /// Returns null if not found (should not happen if EnsureActiveCycleAsync was called)
    /// </summary>
    public async Task<ReportingCycle?> GetActiveCycleAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        return await _db.ReportingCycles
            .Where(c => c.StartDate.Date == monthStart && c.EndDate.Date == monthEnd)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Ensures the current month's cycle exists, creating if necessary.
    /// Deterministic: one cycle per calendar month, deadline = last day of month.
    /// Safe to call repeatedly; idempotent.
    /// </summary>
    public async Task<ReportingCycle> EnsureActiveCycleAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var existing = await _db.ReportingCycles
            .Where(c => c.StartDate.Date == monthStart && c.EndDate.Date == monthEnd)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
            return existing;
        
        var cycle = new ReportingCycle
        {
            CycleMonth = monthStart.ToString("MMMM yyyy"),
            StartDate = monthStart,
            EndDate = monthEnd,
            SubmissionDeadline = monthEnd,
            IsLocked = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.ReportingCycles.Add(cycle);
        await _db.SaveChangesAsync(ct);
        return cycle;
    }

    /// <summary>
    /// Ensures cycle is open for editing, locks if past deadline
    /// Throws if cycle is locked
    /// </summary>
    public async Task EnsureCycleOpenForEditsAsync(int cycleId, CancellationToken ct = default)
    {
        var cycle = await _db.ReportingCycles.FirstOrDefaultAsync(c => c.Id == cycleId, ct)?? throw new InvalidOperationException($"Reporting cycle {cycleId} was not found.");

        var isPastDeadline = DateTime.UtcNow.Date > cycle.SubmissionDeadline.Date;
        if (isPastDeadline && !cycle.IsLocked)
        {
            cycle.IsLocked = true;
            cycle.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        if (cycle.IsLocked || isPastDeadline)
            throw new InvalidOperationException($"This reporting cycle is locked. Submission deadline was {cycle.SubmissionDeadline:MMMM d, yyyy}.");
    }

    #endregion

    #region Utilities

    private static void ValidateJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("Report data JSON cannot be empty.", nameof(json));

        try
        {
            JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Invalid JSON format in report data.", nameof(json), ex);
        }
    }

    private static void ValidateStateReportForm(StateReportForm form)
    {
        if (form.UnitPresidentsAttended < 0 || form.TotalUnitPresidents < 0)
            throw new InvalidOperationException("Attendance values cannot be negative.");
        

        if (form.UnitPresidentsAttended > form.TotalUnitPresidents)
            throw new InvalidOperationException("Unit presidents attended cannot exceed total unit presidents.");
        
        if (form.UnitPerformanceRating is < 0 or > 100)
            throw new InvalidOperationException("Unit performance rating must be between 0 and 100.");
        

        foreach (var program in form.Programs)
        {
            if (program.TotalAttendance < 0 || program.TotalBeneficiaries < 0)
            {
                throw new InvalidOperationException("Program attendance and beneficiaries cannot be negative.");
            }
        }
    }

    private async Task<int?> ResolveCycleIdAsync(int? cycleId, CancellationToken ct)
    {
        if (cycleId.HasValue)
        {
            var explicitCycle = await _db.ReportingCycles.FirstOrDefaultAsync(c => c.Id == cycleId.Value, ct);
            return explicitCycle?.Id;
        }

        var cycle = await GetActiveCycleAsync(ct);
        return cycle?.Id;
    }

    private async Task RecomputeStateAggregateAsync(StateReport stateReport, CancellationToken ct)
    {
        var stateReportsQuery = _db.Reports
            .AsNoTracking()
            .Where(r => r.StateId == stateReport.StateId && r.CycleId == stateReport.CycleId);

        var allUnitReports = await stateReportsQuery.ToListAsync(ct);
        var submittedToStateOrHigher = allUnitReports
            .Where(r => r.Status is ReportStatus.SubmittedToState or ReportStatus.SubmittedToNational or ReportStatus.Acknowledged)
            .ToList();

        stateReport.TotalUnitReportsCount = allUnitReports.Count;
        stateReport.UnitsAttendedTo = submittedToStateOrHigher.Count;
        stateReport.UnitPerformanceRating = stateReport.TotalUnitReportsCount == 0
            ? 0
            : (int)Math.Round((stateReport.UnitsAttendedTo * 100.0) / stateReport.TotalUnitReportsCount);

        var reportIds = allUnitReports.Select(r => r.Id).ToList();
        var departmentRows = reportIds.Count == 0
            ? new List<DepartmentReport>()
            : await _db.DepartmentReports
                .AsNoTracking()
                .Where(dr => reportIds.Contains(dr.ReportId) && dr.IsSubmitted)
                .ToListAsync(ct);

        var groupedByDepartment = departmentRows
            .GroupBy(dr => dr.Department)
            .OrderBy(g => g.Key.ToString())
            .ToList();

        // Remove only previously auto-aggregated program rows so manual edits are preserved
        var existingPrograms = await _db.StateReportPrograms
            .Where(p => p.StateReportId == stateReport.Id)
            .ToListAsync(ct);

        var autoExisting = existingPrograms.Where(p => p.IsAutoAggregated).ToList();
        if (autoExisting.Count > 0)
            _db.StateReportPrograms.RemoveRange(autoExisting);

        var aggregatePrograms = groupedByDepartment.Select(group => new StateReportProgram
        {
            StateReportId = stateReport.Id,
            ProgramName = group.Key.ToString(),
            Objectives = "Auto-aggregated from submitted unit department reports.",
            Outcomes = $"Submitted unit reports: {group.Count()}",
            TotalAttendance = group.Sum(x => x.AttendanceCount ?? x.MemberParticipantCount ?? 0),
            TotalBeneficiaries = group.Sum(x => x.BeneficiaryCount ?? 0),
            IsAutoAggregated = true
        }).ToList();

        if (aggregatePrograms.Count > 0)
        {
            _db.StateReportPrograms.AddRange(aggregatePrograms);
        }

        // mark aggregation metadata and persist
        stateReport.IsAggregated = true;
        stateReport.LastAggregatedAt = DateTime.UtcNow;
        stateReport.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    #endregion
}

#endregion

#region CurrentUserReportService

/// <summary>
/// Convenience service for accessing report operations as the currently authenticated user
/// Wraps UnifiedReportService with automatic user context retrieval
/// Reduces boilerplate in components
/// </summary>
public class CurrentUserReportService(AMSAAuthStateProvider authStateProvider,UnifiedReportService reportService)
{
    private readonly AMSAAuthStateProvider _authStateProvider = authStateProvider;
    private readonly UnifiedReportService _reportService = reportService;

    #region Reporting Cycle & Draft Management

    public Task<ReportingCycle> EnsureActiveCycleAsync(CancellationToken ct = default) => _reportService.EnsureActiveCycleAsync(ct);

    /// <summary>
    /// Query-only method: retrieves the current user's draft for a cycle without creating one
    /// Returns null if no draft exists
    /// </summary>
    public Task<Report?> GetCurrentUserDraftAsync(int cycleId, CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor => 
            _reportService.GetDraftAsync(actor, actor.UnitId, cycleId, ct));

    /// <summary>
    /// Ensures current user has a draft for the given cycle, creating if necessary
    /// Use when user explicitly requests to create or edit a report
    /// </summary>
    public Task<Report> CreateCurrentUserDraftAsync(int cycleId, CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor =>
            _reportService.EnsureDraftAsync(actor, actor.UnitId, cycleId, ct));

    /// <summary>
    /// Backward-compatible alias for explicit draft creation.
    /// Prefer CreateCurrentUserDraftAsync for new call sites.
    /// </summary>
    public Task<Report> EnsureCurrentUserDraftAsync(int cycleId, CancellationToken ct = default) =>
        CreateCurrentUserDraftAsync(cycleId, ct);

    #endregion

    #region Department Report Operations

    public async Task<DepartmentReport?> GetDepartmentAsync(int reportId, DepartmentType department, CancellationToken ct = default)
    {
        var report = await _reportService.GetReportAsync(reportId, ct);
        return report?.DepartmentReports.FirstOrDefault(d => d.Department == department);
    }

    public Task<DepartmentReport> SaveDepartmentJsonAsync(int reportId,DepartmentType department,string reportDataJson,bool markSubmitted,CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor =>
            _reportService.SaveDepartmentDataAsync(reportId, department, reportDataJson, actor, markSubmitted, ct));

    #endregion

    #region Report Retrieval

    public async Task<List<ReportRetrievalSummary>> GetCurrentUserReportHistoryAsync(CancellationToken ct = default)
    {
        var actor = GetCurrentUserOrThrow();
        var reports = await _reportService.GetUnitReportHistoryAsync(actor, actor.UnitId, ct);
        return [.. reports.Select(MapReportSummary)];
    }

    public async Task<ReportRetrievalDetails?> GetReportDetailsAsync(int reportId, CancellationToken ct = default)
    {
        var actor = GetCurrentUserOrThrow();
        var report = await _reportService.GetReportAsync(reportId, ct);
        if (report is null)
            return null;
        
        var access = new ReportAccessService();
        var canView = report.UnitId == actor.UnitId
            || access.CanReviewAtUnitLevel(actor, report.UnitId)
            || access.CanReviewAtStateLevel(actor, report.StateId)
            || access.IsNationalLeadership(actor)
            || actor.HasSudoAccess;

        if (!canView)
            throw new UnauthorizedAccessException("You are not allowed to view this report.");
        

        return MapReportDetails(report);
    }

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
        ExecuteAsCurrentUserAsync(actor =>
            _reportService.SubmitReportToPresidentAsync(actor, reportId, notes, ct));

    #endregion

    #region Unit/State/National Report Views & Leadership Actions

    public Task<List<Report>> GetMyUnitReportsAsync(int? cycleId = null, CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor => 
            _reportService.GetUnitReportsAsync(actor, actor.UnitId, cycleId, ct));

    public Task<List<Report>> GetMyStateReportsAsync(int? cycleId = null, CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor => 
            _reportService.GetStateReportsAsync(actor, actor.StateId, cycleId, ct));

    public Task<List<Report>> GetStateUnitReportsAsync(int? cycleId = null, CancellationToken ct = default) =>
        GetMyStateReportsAsync(cycleId, ct);

    public Task<StateReport?> GetMyStateReportAsync(int cycleId, CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor => 
            _reportService.GetStateReportAsync(actor, actor.StateId, cycleId, ct));

    public Task<StateReport> CreateMyStateReportAsync(int cycleId, CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor => 
            _reportService.EnsureStateReportAsync(actor, actor.StateId, cycleId, ct));

    public Task<StateReport> EnsureMyStateReportAsync(int cycleId, CancellationToken ct = default) =>
        CreateMyStateReportAsync(cycleId, ct);

    public Task<StateReport> SaveMyStateReportAsync(int stateReportId, StateReportForm form, bool markSubmitted, CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor => 
            _reportService.SaveStateReportAsync(actor, stateReportId, form, markSubmitted, ct));

    public Task<List<Report>> GetNationalReportsAsync(int? cycleId = null, CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor => 
            _reportService.GetNationalReportsAsync(actor, cycleId, ct));

    public Task<List<ReportWithStateContext>> GetNationalReportsWithStateContextAsync(int? cycleId = null, CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor => 
            _reportService.GetNationalReportsWithStateContextAsync(actor, cycleId, ct));

    #endregion

    #region Leadership Approval & Rejection

    public Task<Report> ApproveAtUnitAsync(int reportId, string? notes = null, CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor =>
            _reportService.ApproveByUnitLeadershipAsync(actor, reportId, notes, ct));

    public Task<Report> RejectAtUnitAsync(int reportId, string? notes = null, CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor => 
            _reportService.RejectByUnitLeadershipAsync(actor, reportId, notes, ct));

    public Task<Report> ApproveAtStateAsync(int reportId, string? notes = null, CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor => 
            _reportService.ApproveByStateLeadershipAsync(actor, reportId, notes, ct));

    public Task<Report> RejectAtStateAsync(int reportId, string? notes = null, CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor => 
            _reportService.RejectByStateLeadershipAsync(actor, reportId, notes, ct));

    public Task<Report> AcknowledgeAtNationalAsync(int reportId, string? notes = null, CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor => 
            _reportService.AcknowledgeByNationalAsync(actor, reportId, notes, ct));

    #endregion

    #region Private Helpers

    private static ReportRetrievalSummary MapReportSummary(Report report) =>
        new(
            report.Id,
            report.Cycle?.CycleMonth ?? $"Cycle {report.CycleId}",
            report.Status,
            report.CreatedAt,
            report.UpdatedAt,
            report.SubmittedToPresidentAt,
            report.DepartmentReports.Count,
            report.DepartmentReports.Count(d => d.IsSubmitted));

    private static ReportRetrievalDetails MapReportDetails(Report report) =>
        new(
            report.Id,
            report.Cycle?.CycleMonth ?? $"Cycle {report.CycleId}",
            report.Status,
            report.CreatedAt,
            report.UpdatedAt,
            report.SubmittedToPresidentAt,
            report.PresidentialNotes,
            report.StateNotes,
            report.NationalNotes,
            [.. report.DepartmentReports
                .OrderBy(d => d.Department)
                .Select(d => new ReportDepartmentView(d.Department, d.Department.ToString(), d.IsSubmitted, d.SubmittedAt, d.ReportData))],
            [.. report.ActivityLogs
                .OrderByDescending(log => log.ActionAt)
                .Select(log => new ReportActivityView(log.ActionAt, log.Action, log.Notes))]);

    private AuthContext GetCurrentUserOrThrow()
    {
        var user = _authStateProvider.GetCurrentUser();
        if (user is null || !user.IsAuthenticated)
            throw new UnauthorizedAccessException("Login required.");

        return user;
    }

    private Task<T> ExecuteAsCurrentUserAsync<T>(Func<AuthContext, Task<T>> operation) =>
        operation(GetCurrentUserOrThrow());

    private T ExecuteAsCurrentUser<T>(Func<AuthContext, T> operation) =>
        operation(GetCurrentUserOrThrow());

    #endregion
}

#endregion
