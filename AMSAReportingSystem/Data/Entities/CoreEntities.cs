namespace AMSAReportingSystem.Data.Entities;

/// <summary>
/// Represents a reporting cycle (monthly reporting period)
/// </summary>
public class ReportingCycle
{
    public int Id { get; set; }
    public string CycleMonth { get; set; } = string.Empty; // "January 2025"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime SubmissionDeadline { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? ReminderSentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Report> Reports { get; set; } = new List<Report>();
    public ICollection<StateReport> StateReports { get; set; } = new List<StateReport>();
}

/// <summary>
/// Represents a unit's monthly report (encompasses all 9 departments)
/// </summary>
public class Report
{
    public int Id { get; set; }
    public int UnitId { get; set; }
    public int StateId { get; set; }
    public int CycleId { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Draft;
    public bool IsCompliant { get; set; }
    
    // Submission tracking
    public DateTime? SubmittedToPresidentAt { get; set; }
    public int? SubmittedByMemberId { get; set; }
    
    // Presidential approval
    public DateTime? ApprovedByPresidentAt { get; set; }
    public int? ApprovedByPresidentMemberId { get; set; }
    public string? PresidentialNotes { get; set; }
    
    // State GS approval
    public DateTime? ApprovedByStateAt { get; set; }
    public int? ApprovedByStateMemberId { get; set; }
    public string? StateNotes { get; set; }
    
    // National acknowledgment
    public DateTime? AcknowledgedByNationalAt { get; set; }
    public int? AcknowledgedByNationalMemberId { get; set; }
    public string? NationalNotes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Foreign keys
    public ReportingCycle Cycle { get; set; } = null!;

    // Navigation properties
    public ICollection<DepartmentReport> DepartmentReports { get; set; } = new List<DepartmentReport>();
    public ICollection<ReportActivityLog> ActivityLogs { get; set; } = new List<ReportActivityLog>();
}

/// <summary>
/// Tracks submission status and data for each of the 9 departments per unit per cycle
/// </summary>
public class DepartmentReport
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public int? CycleId { get; set; } // denormalized for analytics
    public DepartmentType Department { get; set; }
    /// <summary>
    /// Department-specific payload stored as JSON text.
    /// This avoids schema churn for changing report questions/fields.
    /// </summary>
    public string? ReportData { get; set; }

    // Extracted analytics/compliance fields (hybrid model with ReportData JSON)
    public int? SessionsOrganized { get; set; }
    public int? AttendanceCount { get; set; }
    public int? TotalMemberCount { get; set; }
    public bool? HasOnCampusActivity { get; set; }
    public int? ProgramCount { get; set; }
    public int? MemberParticipantCount { get; set; }
    public decimal? DuesCollected { get; set; }
    public decimal? ExpectedDues { get; set; }
    public int? BeneficiaryCount { get; set; }
    public bool IsSubmitted { get; set; } = false;
    public bool IsCompliant { get; set; } = false;
    public DateTime? SubmittedAt { get; set; }
    public int? SubmittedByMemberId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Foreign keys
    public Report Report { get; set; } = null!;

}

/// <summary>
/// Represents a role with department and level type
/// </summary>
public class Role
{
    public string DepartmentName { get; set; } = string.Empty;
    public LevelType LevelType { get; set; }

    public (string DepartmentName, LevelType LevelType) ParseRole()
    {
        return (DepartmentName, LevelType);
    }
}
