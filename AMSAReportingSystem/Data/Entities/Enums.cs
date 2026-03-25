namespace AMSAReportingSystem.Data.Entities;

/// <summary>
/// Report status throughout the approval workflow
/// </summary>
public enum ReportStatus
{
    Draft = 0,
    SubmittedToPresident = 1,
    RejectedByPresident = 2,
    ApprovedByPresident = 3,
    SubmittedToState = 4,
    RejectedByState = 5,
    ApprovedByState = 6,
    SubmittedToNational = 7,
    Acknowledged = 8
}

/// <summary>
/// Nine department types in AMSA
/// </summary>
public enum DepartmentType
{
    Taleem = 0,           // Islamic Education
    Tabligh = 1,          // Preaching/Missionary Work
    Welfare = 2,          // Welfare/Social Services
    Sport = 3,            // Sports & Recreation
    Finance = 4,          // Finance
    Health = 5,           // Health
    SecondarySchool = 6,  // Secondary School Education
    Tajneed = 7,          // Member Registry/Audit
    General = 8           // General/Miscellaneous
}

/// <summary>
/// Notification types for different events
/// </summary>
public enum NotificationType
{
    ReminderDue = 0,
    ReportSubmitted = 1,
    ReportApproved = 2,
    ReportRejected = 3,
    DeadlineApproaching = 4,
    CycleLocked = 5,
    ComplianceWarning = 6,
    EscalationRequired = 7
}
