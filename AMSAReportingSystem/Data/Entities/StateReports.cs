namespace AMSAReportingSystem.Data.Entities;

/// <summary>
/// Represents a state's monthly report (state-level aggregation)
/// </summary>
public class StateReport
{
    public int Id { get; set; }
    public int StateId { get; set; }
    public int CycleId { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Draft;
    
    // Metrics
    public int UnitPresidentsAttended { get; set; }
    public int TotalUnitPresidents { get; set; }
    public int? UnitPerformanceRating { get; set; } // 0-100
    
    // Content
    public string? UnitImprovementPlan { get; set; }
    public string? ChallengesFaced { get; set; }
    public string? NationalSupportNeeded { get; set; } // Escalation queue
    public string? OtherNotes { get; set; }
    
    // Presidential approval
    public string? PresidentialNote { get; set; }
    public DateTime? ApprovedByPresidentAt { get; set; }
    public int? ApprovedByPresidentMemberId { get; set; }
    
    // National acknowledgment
    public string? NationalNote { get; set; }
    public DateTime? AcknowledgedByNationalAt { get; set; }
    public int? AcknowledgedByNationalMemberId { get; set; }
    
    // Submission tracking
    public DateTime? SubmittedAt { get; set; }
    public int? SubmittedByMemberId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Computed property
    public decimal UnitPresidentAttendanceRate => 
        TotalUnitPresidents > 0 
            ? (UnitPresidentsAttended * 100m / TotalUnitPresidents) 
            : 0;

    // Foreign keys
    public State State { get; set; } = null!;
    public ReportingCycle Cycle { get; set; } = null!;

    // Navigation properties
    public ICollection<StateReportActivity> Activities { get; set; } = new List<StateReportActivity>();
    public ICollection<StateReportAttachment> Attachments { get; set; } = new List<StateReportAttachment>();
    public ICollection<StateReportActivityLog> ActivityLogs { get; set; } = new List<StateReportActivityLog>();
}

/// <summary>
/// Activities/Programs at state level (Q3)
/// </summary>
public class StateReportActivity
{
    public int Id { get; set; }
    public int StateReportId { get; set; }
    public string ActivityTitle { get; set; } = string.Empty;
    public string? Objectives { get; set; }
    public string? Outcomes { get; set; }
    public int? AttendanceCount { get; set; }
    public int? BeneficiaryCount { get; set; }
    public DateTime? ActivityDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign key
    public StateReport StateReport { get; set; } = null!;
}

/// <summary>
/// File attachments for state reports
/// </summary>
public class StateReportAttachment
{
    public int Id { get; set; }
    public int StateReportId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int UploadedByMemberId { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Foreign key
    public StateReport StateReport { get; set; } = null!;
}

/// <summary>
/// Audit trail for state report actions
/// </summary>
public class StateReportActivityLog
{
    public int Id { get; set; }
    public int StateReportId { get; set; }
    public int ActionByMemberId { get; set; }
    public string Action { get; set; } = string.Empty; // "Submitted", "PresidentApproved", etc.
    public string? Notes { get; set; }
    public DateTime ActionAt { get; set; } = DateTime.UtcNow;

    // Foreign key
    public StateReport StateReport { get; set; } = null!;
}
