using AMSAReportingSystem.Data;
using AMSAReportingSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AMSAReportingSystem.Services;

/// <summary>
/// Unified service for all report-related operations
/// Consolidates DepartmentReportService, StateReportService, and ReportLifecycleService functionality
/// Organized by operational scope: Department Ops, State Ops, Report Lifecycle, and Cycles
/// </summary>
public class UnifiedReportService
{
    private const int DefaultSubmissionGraceDays = 7;

    private readonly AMSAReportingDbContext _db;
    private readonly IAmSaApiClient _amSaApiClient;
    private readonly ReportAccessService _access;
    private readonly ILogger<UnifiedReportService> _logger;

    public UnifiedReportService(
        AMSAReportingDbContext db,
        IAmSaApiClient amSaApiClient,
        ReportAccessService access,
        ILogger<UnifiedReportService> logger)
    {
        _db = db;
        _amSaApiClient = amSaApiClient;
        _access = access;
        _logger = logger;
    }

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
    /// Initializes with Draft status
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
            throw new InvalidOperationException($"Unit with AMSA ID {amsaUnitId} could not be found locally or from AMSA API.");
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

        var unit = await EnsureUnitExistsAsync(amsaUnitId, ct)
            ?? throw new InvalidOperationException($"Unit with AMSA ID {amsaUnitId} could not be found locally or from AMSA API.");

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
    /// Retrieves active reporting cycle
    /// Falls back to most recent unlocked cycle if no active cycle found
    /// </summary>
    public async Task<ReportingCycle?> GetActiveCycleAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;

        var active = await _db.ReportingCycles
            .Where(c => !c.IsLocked && c.StartDate.Date <= today && c.EndDate.Date >= today)
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefaultAsync(ct);

        if (active is not null)
        {
            return active;
        }

        return await _db.ReportingCycles
            .Where(c => !c.IsLocked)
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Ensures active cycle exists, creating if necessary
    /// </summary>
    public async Task<ReportingCycle> EnsureActiveCycleAsync(CancellationToken ct = default)
    {
        var existing = await GetActiveCycleAsync(ct);
        if (existing is not null)
        {
            return existing;
        }

        var today = DateTime.UtcNow.Date;
        var start = new DateTime(today.Year, today.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        var cycle = new ReportingCycle
        {
            CycleMonth = start.ToString("MMMM yyyy"),
            StartDate = start,
            EndDate = end,
            SubmissionDeadline = end.AddDays(DefaultSubmissionGraceDays),
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

    /// <summary>
    /// Validates JSON format
    /// </summary>
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

    /// <summary>
    /// Validates state report form data
    /// </summary>
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

    /// <summary>
    /// Resolves cycle ID, using explicit cycle if provided, otherwise active cycle
    /// </summary>
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

    /// <summary>
    /// Ensures unit exists locally, syncing from AMSA API if needed
    /// </summary>
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

    #endregion
}
