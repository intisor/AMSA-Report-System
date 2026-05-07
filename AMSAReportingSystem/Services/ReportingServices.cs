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
public class UnifiedReportService(
    AMSAReportingDbContext db,
    IAmsaApiClient amSaApiClient,
    ReportAccessService access,
    ILogger<UnifiedReportService> logger)
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
    public async Task<DepartmentReport> SaveDepartmentDataAsync(
        int reportId,
        DepartmentType department,
        string reportDataJson,
        AuthContext actor,
        bool markSubmitted,
        CancellationToken ct = default)
    {
        ValidateJson(reportDataJson);

        var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == reportId, ct)
            ?? throw new InvalidOperationException($"Report {reportId} not found.");

        var unit = await _db.Units.FirstAsync(u => u.Id == report.UnitId, ct);

        if (!_access.CanEditDepartment(actor, unit.Id, unit.StateId, department))
        {
            throw new UnauthorizedAccessException("You are not allowed to edit this department report.");
        }

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
    public async Task<DepartmentReport?> GetDepartmentReportAsync(
        int reportId,
        DepartmentType department,
        CancellationToken ct = default)
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
    public async Task<StateReport?> GetStateReportAsync(
        AuthContext actor,
        int stateId,
        int cycleId,
        CancellationToken ct = default)
    {
        if (!_access.CanReviewAtStateLevel(actor, stateId))
        {
            throw new UnauthorizedAccessException("You are not allowed to manage this state report.");
        }

        return await _db.StateReports
            .Include(sr => sr.Programs)
            .Include(sr => sr.ActivityLogs)
            .FirstOrDefaultAsync(sr => sr.StateId == stateId && sr.CycleId == cycleId, ct);
    }

    /// <summary>
    /// Ensures a state report exists, creating if necessary
    /// Initializes with Draft status and links available submitted department reports
    /// </summary>
    public async Task<StateReport> EnsureStateReportAsync(
        AuthContext actor,
        int stateId,
        int cycleId,
        CancellationToken ct = default)
    {
        var existing = await GetStateReportAsync(actor, stateId, cycleId, ct);
        if (existing is not null)
        {
            return existing;
        }

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

        _db.StateReportActivityLogs.Add(new StateReportActivityLog
        {
            StateReportId = report.Id,
            ActionByMemberId = actor.MemberId,
            Action = "StateReportCreated",
            ActionAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        // Link existing submitted department reports
        await LinkDepartmentReportsToStateReportAsync(report, ct);

        return await _db.StateReports
            .Include(sr => sr.Programs)
            .Include(sr => sr.ActivityLogs)
            .FirstAsync(sr => sr.Id == report.Id, ct);
    }

    /// <summary>
    /// Updates state report with aggregated data and leadership notes
    /// Validates form data and manages program list updates
    /// </summary>
    public async Task<StateReport> SaveStateReportAsync(
        AuthContext actor,
        int stateReportId,
        StateReportForm form,
        bool markSubmitted,
        CancellationToken ct = default)
    {
        var stateReport = await _db.StateReports
            .Include(sr => sr.Programs)
            .FirstOrDefaultAsync(sr => sr.Id == stateReportId, ct)
            ?? throw new InvalidOperationException($"State report {stateReportId} not found.");

        if (!_access.CanReviewAtStateLevel(actor, stateReport.StateId))
        {
            throw new UnauthorizedAccessException("You are not allowed to edit this state report.");
        }

        await EnsureCycleOpenForEditsAsync(stateReport.CycleId, ct);

        if (stateReport.Status is ReportStatus.SubmittedToNational or ReportStatus.Acknowledged)
        {
            throw new InvalidOperationException("This state report has already been submitted and is read-only.");
        }

        ValidateStateReportForm(form);

        stateReport.UnitPresidentsAttended = form.UnitPresidentsAttended;
        stateReport.TotalUnitPresidents = form.TotalUnitPresidents;
        stateReport.UnitPerformanceRating = form.UnitPerformanceRating;
        stateReport.UnitImprovementPlan = form.UnitImprovementPlan;
        stateReport.ChallengesFaced = form.ChallengesFaced;
        stateReport.NationalSupportNeeded = form.NationalSupportNeeded;
        stateReport.OtherNotes = form.OtherNotes;
        stateReport.UpdatedAt = DateTime.UtcNow;

        _db.StateReportPrograms.RemoveRange(stateReport.Programs);
        var programs = form.Programs
            .Where(p => !string.IsNullOrWhiteSpace(p.ProgramName)
                        || !string.IsNullOrWhiteSpace(p.Objectives)
                        || !string.IsNullOrWhiteSpace(p.Outcomes)
                        || p.TotalAttendance.HasValue
                        || p.TotalBeneficiaries.HasValue)
            .Select(p => new StateReportProgram
            {
                StateReportId = stateReport.Id,
                ProgramName = string.IsNullOrWhiteSpace(p.ProgramName) ? "Unnamed Program" : p.ProgramName.Trim(),
                Objectives = p.Objectives,
                Outcomes = p.Outcomes,
                TotalAttendance = p.TotalAttendance,
                TotalBeneficiaries = p.TotalBeneficiaries
            })
            .ToList();
        _db.StateReportPrograms.AddRange(programs);

        if (markSubmitted)
        {
            stateReport.Status = ReportStatus.SubmittedToNational;
            stateReport.SubmittedAt = DateTime.UtcNow;
            stateReport.SubmittedByMemberId = actor.MemberId;

            // Link submitted department reports for audit trail
            await LinkDepartmentReportsToStateReportAsync(stateReport, ct);
        }

        _db.StateReportActivityLogs.Add(new StateReportActivityLog
        {
            StateReportId = stateReport.Id,
            ActionByMemberId = actor.MemberId,
            Action = markSubmitted ? "StateReportSubmittedToNational" : "StateReportSaved",
            ActionAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        return await _db.StateReports
            .Include(sr => sr.Programs)
            .Include(sr => sr.ActivityLogs)
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
            .Include(r => r.Unit)
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
        var unit = await EnsureUnitExistsAsync(amsaUnitId, ct);
        if (unit is null)
        {
            unit = await CreateUnitFromAuthContextAsync(actor, ct);
        }

        if (!_access.CanInitiateReportSubmission(actor, unit.Id, unit.StateId))
        {
            throw new UnauthorizedAccessException("You are not allowed to create or manage reports for this unit.");
        }

        return await _db.Reports
            .Include(r => r.DepartmentReports)
            .Include(r => r.ActivityLogs)
            .FirstOrDefaultAsync(r => r.UnitId == unit.Id && r.CycleId == cycleId, ct);
    }

    /// <summary>
    /// Creates draft report for unit with all departments initialized
    /// </summary>
    public async Task<Report> EnsureDraftAsync(AuthContext actor, int amsaUnitId, int cycleId, CancellationToken ct = default)
    {
        var existing = await GetDraftAsync(actor, amsaUnitId, cycleId, ct);
        if (existing is not null)
        {
            return existing;
        }

        var unit = await EnsureUnitExistsAsync(amsaUnitId, ct);
        if (unit is null)
        {
            unit = await CreateUnitFromAuthContextAsync(actor, ct);
        }

        var report = new Report
        {
            UnitId = unit.Id,
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
        var unit = await _db.Units.FirstOrDefaultAsync(u => u.Id == unitId, ct)
            ?? throw new InvalidOperationException($"Unit {unitId} not found.");

        if (!_access.CanReviewAtUnitLevel(actor, unit.Id))
        {
            throw new UnauthorizedAccessException("You are not allowed to view reports for this unit.");
        }

        var selectedCycleId = await ResolveCycleIdAsync(cycleId, ct);
        if (selectedCycleId is null)
        {
            return [];
        }

        return await _db.Reports
            .Include(r => r.Unit)
            .Include(r => r.Cycle)
            .Include(r => r.DepartmentReports)
            .Where(r => r.UnitId == unitId && r.CycleId == selectedCycleId.Value)
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Retrieves all reports for a state with optional cycle filter
    /// </summary>
    public async Task<List<Report>> GetStateReportsAsync(AuthContext actor, int stateId, int? cycleId = null, CancellationToken ct = default)
    {
        if (!_access.CanReviewAtStateLevel(actor, stateId))
        {
            throw new UnauthorizedAccessException("You are not allowed to view reports for this state.");
        }

        var selectedCycleId = await ResolveCycleIdAsync(cycleId, ct);
        if (selectedCycleId is null)
        {
            return [];
        }

        return await _db.Reports
            .Include(r => r.Unit)
            .Include(r => r.Cycle)
            .Include(r => r.DepartmentReports)
            .Where(r => r.CycleId == selectedCycleId.Value && r.Unit.StateId == stateId)
            .OrderBy(r => r.Unit.Name)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Retrieves all reports nationally with optional cycle filter
    /// National leadership only
    /// </summary>
    public async Task<List<Report>> GetNationalReportsAsync(AuthContext actor, int? cycleId = null, CancellationToken ct = default)
    {
        if (!_access.IsNationalLeadership(actor))
        {
            throw new UnauthorizedAccessException("Only national leadership can view national report board.");
        }

        var selectedCycleId = await ResolveCycleIdAsync(cycleId, ct);
        if (selectedCycleId is null)
        {
            return [];
        }

        return await _db.Reports
            .Include(r => r.Unit)
                .ThenInclude(u => u.State)
            .Include(r => r.Cycle)
            .Include(r => r.DepartmentReports)
            .Where(r => r.CycleId == selectedCycleId.Value)
            .OrderBy(r => r.Unit.State.Name)
            .ThenBy(r => r.Unit.Name)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Submits report from unit to president for review
    /// All departments must be submitted first
    /// </summary>
    public async Task<Report> SubmitReportToPresidentAsync(AuthContext actor, int reportId, string? notes = null, CancellationToken ct = default)
    {
        var report = await _db.Reports
            .Include(r => r.DepartmentReports)
            .FirstOrDefaultAsync(r => r.Id == reportId, ct)
            ?? throw new InvalidOperationException($"Report {reportId} not found.");
        await EnsureCycleOpenForEditsAsync(report.CycleId, ct);

        var unit = await _db.Units.FirstAsync(u => u.Id == report.UnitId, ct);
        if (!_access.CanInitiateReportSubmission(actor, unit.Id, unit.StateId))
        {
            throw new UnauthorizedAccessException("You are not allowed to submit this report.");
        }

        if (report.Status != ReportStatus.Draft && report.Status != ReportStatus.RejectedByPresident)
        {
            throw new InvalidOperationException($"Report {reportId} cannot be submitted from status {report.Status}.");
        }

        if (report.DepartmentReports.Any(d => !d.IsSubmitted))
        {
            throw new InvalidOperationException("All department reports must be submitted before report submission.");
        }

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
        var unit = await _db.Units.FirstAsync(u => u.Id == report.UnitId, ct);

        if (!_access.CanReviewAtUnitLevel(actor, unit.Id))
        {
            throw new UnauthorizedAccessException("You are not allowed to approve this report.");
        }

        if (report.Status != ReportStatus.SubmittedToPresident)
        {
            throw new InvalidOperationException($"Report must be in {ReportStatus.SubmittedToPresident} state for unit approval.");
        }

        report.Status = ReportStatus.SubmittedToState;
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
        var unit = await _db.Units.FirstAsync(u => u.Id == report.UnitId, ct);

        if (!_access.CanReviewAtUnitLevel(actor, unit.Id))
        {
            throw new UnauthorizedAccessException("You are not allowed to reject this report.");
        }

        if (report.Status != ReportStatus.SubmittedToPresident)
        {
            throw new InvalidOperationException($"Report must be in {ReportStatus.SubmittedToPresident} state for rejection.");
        }

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
        var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == reportId, ct)
            ?? throw new InvalidOperationException($"Report {reportId} not found.");
        var unit = await _db.Units.FirstAsync(u => u.Id == report.UnitId, ct);

        if (!_access.CanReviewAtStateLevel(actor, unit.StateId))
        {
            throw new UnauthorizedAccessException("You are not allowed to approve this report at state level.");
        }

        if (report.Status != ReportStatus.SubmittedToState)
        {
            throw new InvalidOperationException($"Report must be in {ReportStatus.SubmittedToState} state for state approval.");
        }

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
        var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == reportId, ct)
            ?? throw new InvalidOperationException($"Report {reportId} not found.");
        var unit = await _db.Units.FirstAsync(u => u.Id == report.UnitId, ct);

        if (!_access.CanReviewAtStateLevel(actor, unit.StateId))
        {
            throw new UnauthorizedAccessException("You are not allowed to reject this report at state level.");
        }

        if (report.Status != ReportStatus.SubmittedToState)
        {
            throw new InvalidOperationException($"Report must be in {ReportStatus.SubmittedToState} state for state rejection.");
        }

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
        {
            throw new UnauthorizedAccessException("Only national leadership can acknowledge reports.");
        }

        var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == reportId, ct)
            ?? throw new InvalidOperationException($"Report {reportId} not found.");

        if (report.Status != ReportStatus.SubmittedToNational)
        {
            throw new InvalidOperationException($"Report must be in {ReportStatus.SubmittedToNational} state for national acknowledgment.");
        }

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
        {
            return existing;
        }

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
        var cycle = await _db.ReportingCycles.FirstOrDefaultAsync(c => c.Id == cycleId, ct)
            ?? throw new InvalidOperationException($"Reporting cycle {cycleId} was not found.");

        var isPastDeadline = DateTime.UtcNow.Date > cycle.SubmissionDeadline.Date;
        if (isPastDeadline && !cycle.IsLocked)
        {
            cycle.IsLocked = true;
            cycle.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        if (cycle.IsLocked || isPastDeadline)
        {
            throw new InvalidOperationException(
                $"This reporting cycle is locked. Submission deadline was {cycle.SubmissionDeadline:MMMM d, yyyy}.");
        }
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
        {
            throw new InvalidOperationException("Attendance values cannot be negative.");
        }

        if (form.UnitPresidentsAttended > form.TotalUnitPresidents)
        {
            throw new InvalidOperationException("Unit presidents attended cannot exceed total unit presidents.");
        }

        if (form.UnitPerformanceRating is < 0 or > 100)
        {
            throw new InvalidOperationException("Unit performance rating must be between 0 and 100.");
        }

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

    private async Task<Unit?> EnsureUnitExistsAsync(int amsaUnitId, CancellationToken ct)
    {
        var existing = await _db.Units.FirstOrDefaultAsync(u => u.AmsaDbUnitId == amsaUnitId, ct);
        if (existing is not null)
        {
            return existing;
        }

        var apiUnit = await _amSaApiClient.GetUnitByIdAsync(amsaUnitId, ct);
        if (!apiUnit.IsSuccess || apiUnit.Data is null)
        {
            _logger.LogWarning("Could not sync unit {AmsaUnitId} from AMSA API: {Error}", amsaUnitId, apiUnit.ErrorMessage);
            return null;
        }

        var unitData = apiUnit.Data;
        var state = await _db.States.FirstOrDefaultAsync(s => s.Id == unitData.StateId, ct);
        if (state is null)
        {
            state = new State
            {
                Id = unitData.StateId,
                Name = unitData.StateName,
                Abbreviation = unitData.StateName.Length >= 3 ? unitData.StateName[..3].ToUpperInvariant() : unitData.StateName.ToUpperInvariant(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.States.Add(state);
        }

        var unit = new Unit
        {
            Name = unitData.UnitName,
            StateId = unitData.StateId,
            AmsaDbUnitId = unitData.UnitId,
            PresidentName = "Unknown",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Units.Add(unit);
        await _db.SaveChangesAsync(ct);
        return unit;
    }

    /// <summary>
    /// Fallback method to create unit from AuthContext when AMSA API sync fails.
    /// Used when a user is authenticated but their unit hasn't been synced from the API.
    /// </summary>
    private async Task<Unit> CreateUnitFromAuthContextAsync(AuthContext actor, CancellationToken ct)
    {
        var state = await _db.States.FirstOrDefaultAsync(s => s.Id == actor.StateId, ct);
        if (state is null)
        {
            state = new State
            {
                Id = actor.StateId,
                Name = actor.StateName,
                Abbreviation = actor.StateName.Length >= 3 ? actor.StateName[..3].ToUpperInvariant() : actor.StateName.ToUpperInvariant(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.States.Add(state);
        }

        var unit = new Unit
        {
            Name = actor.UnitName,
            StateId = actor.StateId,
            AmsaDbUnitId = actor.UnitId,
            PresidentName = "Unknown",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Units.Add(unit);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Created unit {UnitId} ({UnitName}) from AuthContext", actor.UnitId, actor.UnitName);
        return unit;
    }

    /// <summary>
    /// Links all submitted department reports from units in a state to the state report
    /// Used for rollup aggregation and audit trail
    /// </summary>
    private async Task LinkDepartmentReportsToStateReportAsync(StateReport stateReport, CancellationToken ct)
    {
        // Get all units in this state
        var unitIds = await _db.Units
            .Where(u => u.StateId == stateReport.StateId)
            .Select(u => u.Id)
            .ToListAsync(ct);

        if (unitIds.Count == 0)
            return;

        // Get all submitted department reports for these units in this cycle
        var submittedDepartmentReports = await _db.DepartmentReports
            .Where(dr => _db.Reports
                .Where(r => r.CycleId == stateReport.CycleId && unitIds.Contains(r.UnitId))
                .Select(r => r.Id)
                .Contains(dr.ReportId) && dr.IsSubmitted)
            .ToListAsync(ct);

        // Remove existing links
        var existingLinks = await _db.StateReportDepartmentData
            .Where(srd => srd.StateReportId == stateReport.Id)
            .ToListAsync(ct);
        _db.StateReportDepartmentData.RemoveRange(existingLinks);

        // Create new links
        var newLinks = submittedDepartmentReports
            .Select(dr => new StateReportDepartmentData
            {
                StateReportId = stateReport.Id,
                DepartmentReportId = dr.Id,
                AddedAt = DateTime.UtcNow
            })
            .ToList();

        _db.StateReportDepartmentData.AddRange(newLinks);
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
public class CurrentUserReportService(
    AMSAAuthStateProvider authStateProvider,
    UnifiedReportService reportService)
{
    private readonly AMSAAuthStateProvider _authStateProvider = authStateProvider;
    private readonly UnifiedReportService _reportService = reportService;

    #region Reporting Cycle & Draft Management

    public Task<ReportingCycle> EnsureActiveCycleAsync(CancellationToken ct = default) =>
        _reportService.EnsureActiveCycleAsync(ct);

    /// <summary>
    /// Query-only method: retrieves the current user's draft for a cycle without creating one
    /// Returns null if no draft exists
    /// </summary>
    public Task<Report?> GetCurrentUserDraftAsync(int cycleId, CancellationToken ct = default) =>
        ExecuteAsCurrentUserAsync(actor => _reportService.GetDraftAsync(actor, actor.UnitId, cycleId, ct));

    /// <summary>
    /// Ensures current user has a draft for the given cycle, creating if necessary
    /// Use when user explicitly requests to create or edit a report
    /// </summary>
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

#endregion
