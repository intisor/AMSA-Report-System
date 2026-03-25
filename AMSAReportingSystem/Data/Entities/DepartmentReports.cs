namespace AMSAReportingSystem.Data.Entities;

/// <summary>
/// Islamic Education (Taleem) Department Report
/// Compliance: 4+ sessions/month AND 75%+ attendance
/// </summary>
public class TaleemReport
{
    public int Id { get; set; }
    public int DepartmentReportId { get; set; }
    public int AverageAttendanceCount { get; set; }
    public int TotalMemberCount { get; set; }
    public string? AttendanceBelowThresholdReason { get; set; }
    public int SessionsOrganized { get; set; }
    public int SessionDurationMinutes { get; set; }
    public int MonthlyTestCount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Computed compliance properties
    public bool IsSessionsCompliant => SessionsOrganized >= 4;
    public bool IsAttendanceCompliant => TotalMemberCount > 0 && (AverageAttendanceCount * 100 / TotalMemberCount) >= 75;
    public bool IsOverallCompliant => IsSessionsCompliant && IsAttendanceCompliant;

    // Foreign key
    public DepartmentReport DepartmentReport { get; set; } = null!;
}

/// <summary>
/// Preaching (Tabligh) Department Report
/// Compliance: At least 1 on-campus tabligh exercise
/// </summary>
public class TablighReport
{
    public int Id { get; set; }
    public int DepartmentReportId { get; set; }
    public string? ActivitiesDetail { get; set; }
    public string? Purpose { get; set; }
    public int AttendanceCount { get; set; }
    public string? Beneficiaries { get; set; }
    public bool HasOnCampusActivity { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Computed compliance property
    public bool IsCompliant => HasOnCampusActivity;

    // Foreign key
    public DepartmentReport DepartmentReport { get; set; } = null!;
}

/// <summary>
/// Welfare Department Report
/// Compliance: 2+ activities
/// </summary>
public class WelfareReport
{
    public int Id { get; set; }
    public int DepartmentReportId { get; set; }
    public int ProgramCount { get; set; }
    public int ParticipantCount { get; set; }
    public string? ActivitiesWithDates { get; set; }
    public string? BriefReport { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Computed compliance property
    public bool IsCompliant => ProgramCount >= 2;

    // Foreign key
    public DepartmentReport DepartmentReport { get; set; } = null!;
}

/// <summary>
/// Sports Department Report
/// Compliance: 75%+ member participation
/// </summary>
public class SportReport
{
    public int Id { get; set; }
    public int DepartmentReportId { get; set; }
    public string? GamesPlayed { get; set; }
    public int MemberParticipantCount { get; set; }
    public int NonMemberParticipantCount { get; set; }
    public int TotalMemberCount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Computed compliance property
    public bool IsAttendanceCompliant => TotalMemberCount > 0 && (MemberParticipantCount * 100 / TotalMemberCount) >= 75;

    // Foreign key
    public DepartmentReport DepartmentReport { get; set; } = null!;
}

/// <summary>
/// Finance Department Report
/// Compliance: Full collection or flagged with reason
/// </summary>
public class FinanceReport
{
    public int Id { get; set; }
    public int DepartmentReportId { get; set; }
    public decimal DuesCollected { get; set; }
    public decimal ExpectedDuesAmount { get; set; }
    public string? DefaultersReason { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Computed compliance properties
    public decimal CollectionRate => ExpectedDuesAmount > 0 ? (DuesCollected / ExpectedDuesAmount) * 100 : 0;
    public bool IsFullyCollected => CollectionRate >= 100;

    // Foreign key
    public DepartmentReport DepartmentReport { get; set; } = null!;
}

/// <summary>
/// Health Department Report
/// Compliance: Descriptive only (no threshold)
/// </summary>
public class HealthReport
{
    public int Id { get; set; }
    public int DepartmentReportId { get; set; }
    public string? ActivitiesWithDates { get; set; }
    public int BeneficiaryCount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Foreign key
    public DepartmentReport DepartmentReport { get; set; } = null!;
}

/// <summary>
/// Secondary School Department Report
/// Compliance: Descriptive only (no threshold)
/// </summary>
public class SecondarySchoolReport
{
    public int Id { get; set; }
    public int DepartmentReportId { get; set; }
    public bool AttendedJamaatMeeting { get; set; }
    public string? ParentSupportEfforts { get; set; }
    public string? JambiteParticipation { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Foreign key
    public DepartmentReport DepartmentReport { get; set; } = null!;
}

/// <summary>
/// Tajneed (Member Registry) Department Report
/// Compliance: Descriptive only (no threshold)
/// </summary>
public class TajneedReport
{
    public int Id { get; set; }
    public int DepartmentReportId { get; set; }
    public bool HasAccurateTajneed { get; set; }
    public string? ImprovementEfforts { get; set; }
    public string? AccuracyPlans { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Foreign key
    public DepartmentReport DepartmentReport { get; set; } = null!;
}

/// <summary>
/// General (Miscellaneous) Department Report
/// Compliance: Descriptive only (no threshold)
/// </summary>
public class GeneralReport
{
    public int Id { get; set; }
    public int DepartmentReportId { get; set; }
    public string? ChallengesFaced { get; set; }
    public string? OtherActivities { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Foreign key
    public DepartmentReport DepartmentReport { get; set; } = null!;
}
