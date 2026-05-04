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
    Taleem = 0,           
    Tabligh = 1,          
    Welfare = 2,          
    Sport = 3,            
    Finance = 4,          
    Health = 5,           
    SecondarySchool = 6,  
    Tajneed = 7,          
    General = 8,
    President = 9,
    VicePresident = 10,
}

/// <summary>
/// 3 levels of organizational hierarchy in AMSA
/// </summary>
public enum LevelType
{
    Unit = 0,
    State = 1,
    National = 2
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
