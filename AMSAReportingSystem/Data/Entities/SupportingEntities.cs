namespace AMSAReportingSystem.Data.Entities;

/// <summary>
/// Audit trail for unit report actions
/// </summary>
public class ReportActivityLog
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public int ActionByMemberId { get; set; }
    public string Action { get; set; } = string.Empty; // "DepartmentSubmitted:Taleem", "PresidentApproved", etc.
    public string? Notes { get; set; }
    public DateTime ActionAt { get; set; } = DateTime.UtcNow;

    // Foreign key
    public Report Report { get; set; } = null!;
}

/// <summary>
/// File attachments for department reports
/// </summary>
public class ReportAttachment
{
    public int Id { get; set; }
    public int DepartmentReportId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // "image/jpeg", "application/pdf", etc.
    public long FileSizeBytes { get; set; }
    public int UploadedByMemberId { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Foreign key
    public DepartmentReport DepartmentReport { get; set; } = null!;
}

/// <summary>
/// In-app notifications for users
/// </summary>
public class Notification
{
    public int Id { get; set; }
    public int RecipientMemberId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    
    // Optional foreign keys (for linking to related entities)
    public int? ReportId { get; set; }
    public int? StateReportId { get; set; }
    public int? CycleId { get; set; }
    
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Report? Report { get; set; }
    public StateReport? StateReport { get; set; }
    public ReportingCycle? Cycle { get; set; }
}

/// <summary>
/// Compliance check results (for audit trail)
/// </summary>
public class ComplianceCheck
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public DepartmentType Department { get; set; }
    public bool IsCompliant { get; set; }
    public string ComplianceNotes { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    // Foreign key
    public Report Report { get; set; } = null!;
}

/// <summary>
/// System-wide reminders for deadlines and follow-ups
/// </summary>
public class Reminder
{
    public int Id { get; set; }
    public int CycleId { get; set; }
    public string ReminderType { get; set; } = string.Empty; // "SubmissionDeadlineApproaching", "DeadlineReached", etc.
    public string? TargetRole { get; set; } // "Officer", "President", "StateGS", "NationalLeadership", null = all
    public string? TargetState { get; set; } // null = national, or specific state code
    public string Message { get; set; } = string.Empty;
    public DateTime ScheduledFor { get; set; }
    public DateTime? SentAt { get; set; }
    public bool IsProcessed { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign key
    public ReportingCycle Cycle { get; set; } = null!;
}
