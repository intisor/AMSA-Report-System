using AMSAReportingSystem.Data;
using AMSAReportingSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AMSAReportingSystem.Services;

public class ReportService
{
    private const int DefaultSubmissionGraceDays = 7;
    private readonly AmsaReportingDbContext _db;
    private readonly IAmSaApiClient _amSaApiClient;
    private readonly ReportAccessService _access;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        AmsaReportingDbContext db,
        IAmSaApiClient amSaApiClient,
        ReportAccessService access,
        ILogger<ReportService> logger)
    {
        _db = db;
        _amSaApiClient = amSaApiClient;
        _access = access;
        _logger = logger;
    }

    public async Task<Report> GetOrCreateDraftAsync(AuthContext actor, int amsaUnitId, int cycleId, CancellationToken ct = default)
    {
        var unit = await EnsureUnitExistsAsync(amsaUnitId, ct);
        if (unit is null)
        {
            throw new InvalidOperationException($"Unit with AMSA ID {amsaUnitId} could not be found locally or from AMSA API.");
        }

        if (!_access.CanSubmitReportToPresident(actor, unit.Id, unit.StateId))
        {
            throw new UnauthorizedAccessException("You are not allowed to create or manage reports for this unit.");
        }

        var existing = await _db.Reports
            .Include(r => r.DepartmentReports)
            .Include(r => r.ActivityLogs)
            .FirstOrDefaultAsync(r => r.UnitId == unit.Id && r.CycleId == cycleId, ct);

        if (existing is not null)
        {
            return existing;
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

        foreach (var department in Enum.GetValues<DepartmentType>())
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

    public async Task<ReportingCycle> GetOrCreateActiveCycleAsync(CancellationToken ct = default)
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

        var latestUnlocked = await _db.ReportingCycles
            .Where(c => !c.IsLocked)
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefaultAsync(ct);

        if (latestUnlocked is not null)
        {
            return latestUnlocked;
        }

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

    public async Task<Report?> GetReportAsync(int reportId, CancellationToken ct = default)
    {
        return await _db.Reports
            .Include(r => r.Unit)
            .Include(r => r.Cycle)
            .Include(r => r.DepartmentReports)
            .Include(r => r.ActivityLogs)
            .FirstOrDefaultAsync(r => r.Id == reportId, ct);
    }

    public async Task<List<Report>> GetUnitReportsAsync(AuthContext actor, int unitId, int? cycleId = null, CancellationToken ct = default)
    {
        var unit = await _db.Units.FirstOrDefaultAsync(u => u.Id == unitId, ct)
            ?? throw new InvalidOperationException($"Unit {unitId} not found.");

        if (!_access.CanReviewAtUnitLevel(actor, unit.Id))
        {
            throw new UnauthorizedAccessException("You are not allowed to view reports for this unit.");
        }

        var selectedCycleId = cycleId ?? (await GetOrCreateActiveCycleAsync(ct)).Id;

        return await _db.Reports
            .Include(r => r.Unit)
            .Include(r => r.Cycle)
            .Include(r => r.DepartmentReports)
            .Where(r => r.UnitId == unitId && r.CycleId == selectedCycleId)
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<Report>> GetStateReportsAsync(AuthContext actor, int stateId, int? cycleId = null, CancellationToken ct = default)
    {
        if (!_access.CanReviewAtStateLevel(actor, stateId))
        {
            throw new UnauthorizedAccessException("You are not allowed to view reports for this state.");
        }

        var selectedCycleId = cycleId ?? (await GetOrCreateActiveCycleAsync(ct)).Id;

        return await _db.Reports
            .Include(r => r.Unit)
            .Include(r => r.Cycle)
            .Include(r => r.DepartmentReports)
            .Where(r => r.CycleId == selectedCycleId && r.Unit.StateId == stateId)
            .OrderBy(r => r.Unit.Name)
            .ToListAsync(ct);
    }

    public async Task<List<Report>> GetNationalReportsAsync(AuthContext actor, int? cycleId = null, CancellationToken ct = default)
    {
        if (!_access.IsNationalLeadership(actor))
        {
            throw new UnauthorizedAccessException("Only national leadership can view national report board.");
        }

        var selectedCycleId = cycleId ?? (await GetOrCreateActiveCycleAsync(ct)).Id;

        return await _db.Reports
            .Include(r => r.Unit)
                .ThenInclude(u => u.State)
            .Include(r => r.Cycle)
            .Include(r => r.DepartmentReports)
            .Where(r => r.CycleId == selectedCycleId)
            .OrderBy(r => r.Unit.State.Name)
            .ThenBy(r => r.Unit.Name)
            .ToListAsync(ct);
    }

    public async Task<StateReport> GetOrCreateStateReportAsync(AuthContext actor, int stateId, int cycleId, CancellationToken ct = default)
    {
        if (!_access.CanReviewAtStateLevel(actor, stateId))
        {
            throw new UnauthorizedAccessException("You are not allowed to manage this state report.");
        }

        var existing = await _db.StateReports
            .Include(sr => sr.Programs)
            .Include(sr => sr.ActivityLogs)
            .FirstOrDefaultAsync(sr => sr.StateId == stateId && sr.CycleId == cycleId, ct);

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

    public async Task<StateReport?> GetStateReportAsync(int stateReportId, CancellationToken ct = default)
    {
        return await _db.StateReports
            .Include(sr => sr.Programs)
            .Include(sr => sr.ActivityLogs)
            .FirstOrDefaultAsync(sr => sr.Id == stateReportId, ct);
    }

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
        await EnsureCycleOpenForEditsAsync(report.CycleId, ct);
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
                Department = department,
                CreatedAt = DateTime.UtcNow
            };
            _db.DepartmentReports.Add(departmentReport);
        }

        departmentReport.ReportData = reportDataJson;
        departmentReport.CycleId = report.CycleId;
        ApplyExtractedFields(departmentReport, reportDataJson);
        departmentReport.UpdatedAt = DateTime.UtcNow;

        if (markSubmitted)
        {
            departmentReport.IsSubmitted = true;
            departmentReport.SubmittedAt = DateTime.UtcNow;
            departmentReport.SubmittedByMemberId = actor.MemberId;
        }

        _db.ReportActivityLogs.Add(new ReportActivityLog
        {
            ReportId = reportId,
            ActionByMemberId = actor.MemberId,
            Action = markSubmitted ? $"DepartmentSubmitted:{department}" : $"DepartmentSaved:{department}",
            ActionAt = DateTime.UtcNow
        });

        report.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return departmentReport;
    }

    public async Task<Report> SubmitReportToPresidentAsync(AuthContext actor, int reportId, string? notes = null, CancellationToken ct = default)
    {
        var report = await _db.Reports
            .Include(r => r.DepartmentReports)
            .FirstOrDefaultAsync(r => r.Id == reportId, ct)
            ?? throw new InvalidOperationException($"Report {reportId} not found.");
        await EnsureCycleOpenForEditsAsync(report.CycleId, ct);

        var unit = await _db.Units.FirstAsync(u => u.Id == report.UnitId, ct);
        if (!_access.CanSubmitReportToPresident(actor, unit.Id, unit.StateId))
        {
            throw new UnauthorizedAccessException("You are not allowed to submit this report.");
        }

        if (report.Status != ReportStatus.Draft && report.Status != ReportStatus.RejectedByPresident)
        {
            throw new InvalidOperationException($"Report {reportId} cannot be submitted from status {report.Status}.");
        }

        var allSubmitted = report.DepartmentReports.All(d => d.IsSubmitted);
        if (!allSubmitted)
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

    private static void ApplyExtractedFields(DepartmentReport row, string reportDataJson)
    {
        using var doc = JsonDocument.Parse(reportDataJson);
        var root = doc.RootElement;

        row.SessionsOrganized = GetInt(root, "sessionsOrganized");
        row.AttendanceCount = GetInt(root, "attendanceCount");
        row.TotalMemberCount = GetInt(root, "totalMemberCount");
        row.HasOnCampusActivity = GetBool(root, "hasOnCampusActivity");
        row.ProgramCount = GetInt(root, "programCount");
        row.MemberParticipantCount = GetInt(root, "memberParticipantCount");
        row.DuesCollected = GetDecimal(root, "duesCollected");
        row.ExpectedDues = GetDecimal(root, "expectedDues");
        row.BeneficiaryCount = GetInt(root, "beneficiaryCount");

        row.IsCompliant = ComputeCompliance(row);
    }

    private static bool ComputeCompliance(DepartmentReport row)
    {
        return row.Department switch
        {
            DepartmentType.Taleem => (row.SessionsOrganized ?? 0) >= 4 &&
                                     MeetsPercent(row.AttendanceCount, row.TotalMemberCount, 75m),
            DepartmentType.Tabligh => row.HasOnCampusActivity == true,
            DepartmentType.Welfare => (row.ProgramCount ?? 0) >= 2,
            DepartmentType.Sport => MeetsPercent(row.MemberParticipantCount, row.TotalMemberCount, 75m),
            DepartmentType.Finance => (row.ExpectedDues ?? 0m) <= 0m ||
                                      (row.DuesCollected ?? 0m) >= (row.ExpectedDues ?? 0m),
            DepartmentType.Health => true,
            DepartmentType.SecondarySchool => true,
            DepartmentType.Tajneed => true,
            DepartmentType.General => true,
            _ => row.IsCompliant
        };
    }

    private static bool MeetsPercent(int? numerator, int? denominator, decimal percent)
    {
        if (!numerator.HasValue || !denominator.HasValue || denominator.Value <= 0) return false;
        return (numerator.Value * 100m / denominator.Value) >= percent;
    }

    private static int? GetInt(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var prop)) return null;
        return prop.ValueKind switch
        {
            JsonValueKind.Number when prop.TryGetInt32(out var v) => v,
            JsonValueKind.String when int.TryParse(prop.GetString(), out var v) => v,
            _ => null
        };
    }

    private static decimal? GetDecimal(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var prop)) return null;
        return prop.ValueKind switch
        {
            JsonValueKind.Number when prop.TryGetDecimal(out var v) => v,
            JsonValueKind.String when decimal.TryParse(prop.GetString(), out var v) => v,
            _ => null
        };
    }

    private static bool? GetBool(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var prop)) return null;
        return prop.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(prop.GetString(), out var v) => v,
            _ => null
        };
    }

    private static void ValidateJson(string json)
    {
        try
        {
            using var _ = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Invalid JSON payload for department report.", ex);
        }
    }

    private async Task EnsureCycleOpenForEditsAsync(int cycleId, CancellationToken ct)
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
}
