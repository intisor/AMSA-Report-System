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
    public int UnitsAttendedTo { get; set; }
    public int TotalUnitReportsCount { get; set; }
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

    // Aggregation metadata
    public bool IsAggregated { get; set; } = false;
    public DateTime? LastAggregatedAt { get; set; }
    public int? LastAggregatedByMemberId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Computed property
    public decimal UnitPresidentAttendanceRate => 
        TotalUnitReportsCount > 0 
            ? (UnitsAttendedTo * 100m / TotalUnitReportsCount) 
            : 0;

    // Foreign keys
    public ReportingCycle Cycle { get; set; } = null!;

    // Navigation properties
    public ICollection<StateReportProgram> Programs { get; set; } = new List<StateReportProgram>();
}

/// <summary>
/// Normalized programs for state report aggregation/analytics
/// </summary>
public class StateReportProgram
{
    public int ProgramId { get; set; }
    public int StateReportId { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public string? Objectives { get; set; }
    public string? Outcomes { get; set; }
    public int? TotalAttendance { get; set; }
    public int? TotalBeneficiaries { get; set; }

    // Aggregation source flag - true when row was created by auto-aggregation
    public bool IsAutoAggregated { get; set; } = true;

    // Foreign key
    public StateReport StateReport { get; set; } = null!;
}

