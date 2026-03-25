# Backend Architecture Design - AMSA Reporting System

## Overview

The frontend POC has validated the UX. Now we're ready to design the backend that will support:
- Report creation, submission, approval workflows
- Role-based access control with 4 authority levels
- Real-time compliance validation
- Deadline management and reminders
- Complete audit trail

---

## Database Schema (EF Core)

### Core Entities

#### `ReportingCycle`
Represents a monthly reporting period (e.g., January 2025)

```csharp
public class ReportingCycle
{
    public int Id { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime SubmissionDeadline { get; set; }
    public bool IsLocked { get; set; }
    public string? ReportMonth { get; set; } // "January 2025"
    
    public ICollection<Report> Reports { get; set; } = new List<Report>();
    public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
}
```

#### `Unit`
AMSA organizational unit (local mosque/community center)

```csharp
public class Unit
{
    public int Id { get; set; }
    public string Name { get; set; } // e.g., "Ilorin Unit"
    public int StateId { get; set; }
    public State State { get; set; }
    public string PresidentName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    
    public ICollection<Report> Reports { get; set; } = new List<Report>();
}
```

#### `State`
Nigerian state (36 states + FCT = 37)

```csharp
public class State
{
    public int Id { get; set; }
    public string Name { get; set; } // e.g., "Kwara"
    public string Abbreviation { get; set; } // e.g., "KWR"
    public string GeneralSecretaryName { get; set; }
    public string PresidentName { get; set; }
    
    public ICollection<Unit> Units { get; set; } = new List<Unit>();
}
```

#### `Report`
Monthly report for a unit

```csharp
public class Report
{
    public int Id { get; set; }
    public int UnitId { get; set; }
    public Unit Unit { get; set; }
    public int CycleId { get; set; }
    public ReportingCycle Cycle { get; set; }
    
    public string Status { get; set; } // Draft, SubmittedToPresident, ApprovedByPresident, SubmittedToState, ...
    public bool IsCompliant { get; set; }
    
    public DateTime? SubmittedAt { get; set; }
    public int? SubmittedByMemberId { get; set; }
    
    public DateTime? ApprovedByPresidentAt { get; set; }
    public int? ApprovedByPresidentMemberId { get; set; }
    public string? PresidentalNotes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<DepartmentReport> DepartmentReports { get; set; } = new List<DepartmentReport>();
    public ICollection<ComplianceCheck> ComplianceChecks { get; set; } = new List<ComplianceCheck>();
    public ICollection<ReportActivityLog> ActivityLogs { get; set; } = new List<ReportActivityLog>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
```

#### `DepartmentReport`
One of 9 sections within a Report (Taleem, Tabligh, Welfare, etc.)

```csharp
public class DepartmentReport
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public Report Report { get; set; }
    
    public string Department { get; set; } // Taleem, Tabligh, Welfare, Sport, Finance, Health, SecondarySchool, Tajneed, General
    public string Status { get; set; } // Draft, Submitted, Approved
    
    // Department-specific fields (stored as JSON for flexibility)
    public string? FieldsJson { get; set; } // Serialized form data
    
    public bool IsCompliant { get; set; }
    public string? ComplianceMessage { get; set; }
    
    public DateTime? SubmittedAt { get; set; }
    public int? SubmittedByMemberId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<ComplianceCheck> ComplianceChecks { get; set; } = new List<ComplianceCheck>();
}
```

#### `ComplianceCheck`
Result of running compliance validation

```csharp
public class ComplianceCheck
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public Report Report { get; set; }
    public int? DepartmentReportId { get; set; }
    public DepartmentReport? DepartmentReport { get; set; }
    
    public string Department { get; set; } // Which department was checked
    public bool IsCompliant { get; set; }
    public string Messages { get; set; } // JSON array of messages
    
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public int CheckedByMemberId { get; set; } // System or user ID
}
```

#### `ReportActivityLog`
Audit trail of all report changes

```csharp
public class ReportActivityLog
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public Report Report { get; set; }
    
    public string Action { get; set; } // "Created", "Submitted", "Approved", "Escalated", "Rejected"
    public string? Department { get; set; } // If department-specific
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; }
    
    public int? ChangedByMemberId { get; set; }
    public string ChangedByRole { get; set; } // "Officer", "President", "StateGS", "NationalGS"
    
    public string? Notes { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
```

#### `Notification`
In-app notification for users

```csharp
public class Notification
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public Report Report { get; set; }
    
    public int RecipientMemberId { get; set; }
    public string Type { get; set; } // "DeadlineReminder", "SubmissionReceived", "ApprovalNeeded", "Escalation"
    public string Title { get; set; }
    public string Message { get; set; }
    
    public bool IsRead { get; set; } = false;
    public DateTime ReadAt { get; set; }?
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

#### `Reminder`
Scheduled reminder (to be sent via background job)

```csharp
public class Reminder
{
    public int Id { get; set; }
    public int CycleId { get; set; }
    public ReportingCycle Cycle { get; set; }
    
    public int? UnitId { get; set; } // Unit-specific or null for all
    public int? StateId { get; set; } // State-specific or null for all
    
    public string Type { get; set; } // "DeadlineApproaching", "DeadlinePassed", "FollowUp"
    public DateTime ScheduledFor { get; set; }
    public bool IsSent { get; set; } = false;
    public DateTime? SentAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

#### `Member` (AMSA Member - cached from external API)
User account mapping to AMSA system

```csharp
public class Member
{
    public int Id { get; set; }
    public string MkanId { get; set; } // MKAN ID from AMSA
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int? UnitId { get; set; }
    public int? StateId { get; set; }
    
    public List<string> Roles { get; set; } = new(); // JSON array
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
}
```

---

## Report State Machine

```
┌─────────────────────────────────────────────────────────────────┐
│                         UNIT OFFICER                             │
│                       (Department Head)                          │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ 1. Create Report (Draft)                                │   │
│  │    - Initialize 9 department sections                   │   │
│  │    - All sections in "Draft" state                      │   │
│  │    - Save form fields as JSON                           │   │
│  │                                                          │   │
│  │ 2. Fill Department Reports                              │   │
│  │    - Taleem: Sessions, Attendance, Notes                │   │
│  │    - Tabligh: Preaching activities                      │   │
│  │    - Welfare: Programs conducted                        │   │
│  │    - Sport: Games organized                             │   │
│  │    - Finance: Collections, expenses                     │   │
│  │    - Health: Health programs                            │   │
│  │    - SecondarySchool: Jama'at metrics                   │   │
│  │    - Tajneed: Membership validation                     │   │
│  │    - General: Challenges, other activities             │   │
│  │                                                          │   │
│  │ 3. Real-Time Validation (Frontend)                      │   │
│  │    - ComplianceEngine.ValidateTaleem():                │   │
│  │      • Sessions ≥ 4? ✓                                 │   │
│  │      • Attendance ≥ 75%? ✓                             │   │
│  │      • Return IsCompliant, Messages                    │   │
│  │    - Similar rules for other departments                │   │
│  │                                                          │   │
│  │ 4. Submit to President                                  │   │
│  │    - Change status: Draft → SubmittedToPresident       │   │
│  │    - Log activity (who, when, status)                  │   │
│  │    - Create notifications for president                │   │
│  │    - Run ComplianceCheck (save results)                │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                      UNIT PRESIDENT                              │
│                   (Approval Authority)                           │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ 5. Review Report                                        │   │
│  │    - See all 9 departments with compliance badges       │   │
│  │    - See compliance summary (8 compliant, 1 flagged)   │   │
│  │    - Can reject individual sections or entire report   │   │
│  │                                                          │   │
│  │ 6. Approve & Forward                                    │   │
│  │    - Add presidential notes/review                      │   │
│  │    - Change status: SubmittedToPresident → ApprovedBy...   │   │
│  │    - If any non-compliant: Flag for escalation          │   │
│  │    - Log approval (who, when, notes)                    │   │
│  │    - Create notification for State GS                   │   │
│  │                                                          │   │
│  │ 7. Escalate (if needed)                                 │   │
│  │    - Non-compliant sections → Send to State GS          │   │
│  │    - Mark for national review if critical               │   │
│  │    - Log escalation reason                              │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                    STATE GENERAL SECRETARY                       │
│              (Consolidation & Monitoring)                        │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ 8. Monitor Submissions                                  │   │
│  │    - Dashboard shows all 12 units (or more)            │   │
│  │    - Filter by: Submitted, Pending, Compliant, Flagged │   │
│  │    - See compliance % per unit                          │   │
│  │    - Timeline of submissions                            │   │
│  │                                                          │   │
│  │ 9. Review & Consolidate                                 │   │
│  │    - Can comment on unit reports                        │   │
│  │    - Can escalate issues to national                    │   │
│  │    - Prepare state summary report (Q1-Q5)              │   │
│  │      • Q1: Unit president attendance                    │   │
│  │      • Q2: Unit rating (0-100)                         │   │
│  │      • Q3: Programs (with outcomes)                     │   │
│  │      • Q4: Challenges + national support needed        │   │
│  │      • Q5: Additional notes                            │   │
│  │                                                          │   │
│  │ 10. Compile State Report                                │   │
│  │     - Aggregate all unit data                           │   │
│  │     - Calculate state compliance %                      │   │
│  │     - Identify escalations                              │   │
│  │     - Submit to national (status → SubmittedToNational) │   │
│  │     - Log consolidation (who, when)                     │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                    NATIONAL GENERAL SECRETARY                    │
│              (System-Wide Oversight)                             │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ 11. Monitor National Status                             │   │
│  │     - Dashboard: 36 states, 450+ units submitted        │   │
│  │     - Compliance rate: 92%                              │   │
│  │     - Escalations list (4 states with issues)           │   │
│  │     - Submission timeline                               │   │
│  │     - Top performing states                             │   │
│  │                                                          │   │
│  │ 12. Identify Escalations                                │   │
│  │     - Non-compliant units (< 75% compliance)            │   │
│  │     - Missing submissions (past deadline)               │   │
│  │     - Critical issues (escalated by states)             │   │
│  │                                                          │   │
│  │ 13. Generate Reports                                    │   │
│  │     - National compliance summary (PDF)                 │   │
│  │     - By-state analysis                                 │   │
│  │     - Trends & recommendations                          │   │
│  │     - Status → FinalizedByNational                      │   │
│  │                                                          │   │
│  │ 14. Close Cycle                                         │   │
│  │     - Mark ReportingCycle as locked                     │   │
│  │     - Archive all reports                               │   │
│  │     - Begin next cycle                                  │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              ↓
                        ┌──────────┐
                        │ ARCHIVED │
                        │ Jan 2025 │
                        └──────────┘
```

### Status Values
```
Draft                      // Officer creating
SubmittedToPresident       // Officer → President
ApprovedByPresident        // President approved
RejectedByPresident        // President rejected
SubmittedToState           // President → State GS
SubmittedToNational        // State GS → National
FinalizedByNational        // National GS finalized
EscalatedToNational        // Flagged for national review
```

---

## API Endpoints (RESTful Design)

### Authentication
```
POST /api/auth/token
  Body: { mkanId, password }
  Response: { token, user, roles, expiresIn }

POST /api/auth/logout
  Headers: Authorization: Bearer <token>
  Response: { success }

POST /api/auth/refresh
  Body: { refreshToken }
  Response: { token, expiresIn }
```

### Reporting Cycles
```
GET /api/cycles                    // Current + upcoming
GET /api/cycles/{id}               // Single cycle details
POST /api/cycles                   // Create (admin only)
PATCH /api/cycles/{id}/lock        // Lock cycle (prevent submissions)
```

### Reports (CRUD & Workflow)
```
GET /api/reports                   // List user reports
  Query: ?cycleId=X&status=Draft&stateId=X

GET /api/reports/{id}              // Full report with all sections
GET /api/reports/{id}/summary      // Quick summary
POST /api/reports                  // Create new report
  Body: { unitId, cycleId }

PUT /api/reports/{id}              // Update report (draft only)
  Body: { departments: [...], notes: "..." }

DELETE /api/reports/{id}           // Delete (draft only)

POST /api/reports/{id}/submit      // Officer submits to President
  Body: { notes: "..." }
  Response: { newStatus, submittedAt, notifications: [...] }

POST /api/reports/{id}/approve     // President approves
  Body: { presidentNotes: "...", escalateDepartments: [...] }
  Response: { newStatus, approvedAt, notifications: [...] }

POST /api/reports/{id}/reject      // President rejects
  Body: { reason: "..." }
  Response: { newStatus, returnedToDraft, notifications: [...] }

POST /api/reports/{id}/escalate    // Escalate to national
  Body: { reason: "...", departments: [...] }
  Response: { newStatus, notifications: [...] }

PATCH /api/reports/{id}/status     // Manual status change (admin)
  Body: { newStatus: "..." }
```

### Department Reports
```
GET /api/reports/{reportId}/departments
  Response: [
    {
      department: "Taleem",
      status: "Draft",
      isCompliant: true,
      fields: { sessions: 5, attendance: 35, ... },
      lastModified: "2025-01-28T..."
    },
    ...
  ]

GET /api/reports/{reportId}/departments/{dept}
  Response: { department, status, fields, compliance: {...} }

PUT /api/reports/{reportId}/departments/{dept}
  Body: { sessions: 5, attendance: 35, notes: "..." }
  Response: { department, status, isCompliant, messages: [...] }

PATCH /api/reports/{reportId}/departments/{dept}/status
  Body: { newStatus: "Submitted" }
  Response: { status }
```

### Compliance
```
POST /api/compliance/check         // Validate single report
  Body: { reportId }
  Response: [
    { department: "Taleem", isCompliant: true, messages: [...] },
    ...
  ]

POST /api/compliance/check-department
  Body: { department: "Taleem", fields: {...} }
  Response: { isCompliant: true, messages: [...] }

GET /api/compliance/results/{reportId}
  Response: { overallCompliant: true, departments: [...] }

GET /api/compliance/rules         // Fetch compliance thresholds
  Response: {
    "Taleem": { sessions: 4, attendance: 75 },
    "Welfare": { programs: 1, participants: 10 },
    ...
  }
```

### Analytics
```
GET /api/analytics/national        // National dashboard data
  Response: {
    states: 36,
    submitted: 450,
    compliant: 414,
    complianceRate: 92,
    escalations: 4,
    timeline: [...]
  }

GET /api/analytics/state/{stateId} // State dashboard data
  Response: {
    units: 12,
    submitted: 11,
    compliant: 10,
    complianceRate: 91,
    byUnit: [...]
  }

GET /api/analytics/escalations     // List all escalations
  Query: ?stateId=X&severity=critical
  Response: [
    { state: "Oyo", unit: "Ilorin", issue: "Financial crisis", severity: "critical" },
    ...
  ]

GET /api/analytics/trends         // Compliance trends over time
  Query: ?months=12
  Response: {
    byMonth: [
      { month: "Jan 2025", compliant: 92, submitted: 98 },
      ...
    ]
  }
```

### Notifications
```
GET /api/notifications             // List user notifications
  Query: ?unreadOnly=true&limit=10

PATCH /api/notifications/{id}      // Mark as read
  Body: { isRead: true }

DELETE /api/notifications/{id}     // Dismiss

POST /api/notifications/{id}/actions/{action}
  Body: { action: "approved" }     // Custom actions per notification type
```

### Activity Log (Audit Trail)
```
GET /api/reports/{reportId}/activity
  Response: [
    {
      action: "Submitted",
      department: "Taleem",
      oldStatus: "Draft",
      newStatus: "SubmittedToPresident",
      changedBy: "Ahmad Hassan",
      changedByRole: "Officer",
      notes: "Monthly submission",
      changedAt: "2025-01-28T..."
    },
    ...
  ]
```

---

## Service Layer Design

### IComplianceEngine
```csharp
public interface IComplianceEngine
{
    // Validate specific department
    ComplianceResult ValidateTaleem(TaleemReportDto dto);
    ComplianceResult ValidateTabligh(TablighReportDto dto);
    ComplianceResult ValidateWelfare(WelfareReportDto dto);
    // ... 6 more departments
    
    // Validate entire report
    ComplianceReport ValidateFullReport(Report report);
    
    // Bulk validation for state/national view
    List<ComplianceReport> ValidateReports(List<Report> reports);
}
```

### IReportWorkflow
```csharp
public interface IReportWorkflow
{
    Task<Report> CreateReportAsync(int unitId, int cycleId);
    Task<Report> SubmitToPresidentAsync(int reportId, int submittedByMemberId, string notes);
    Task<Report> ApproveByPresidentAsync(int reportId, int approvedByMemberId, string notes);
    Task<Report> RejectByPresidentAsync(int reportId, int rejectedByMemberId, string reason);
    Task<Report> EscalateToNationalAsync(int reportId, string reason, List<string> departments);
    
    // Check permissions
    bool CanSubmit(Report report, Member user);
    bool CanApprove(Report report, Member user);
    bool CanEscalate(Report report, Member user);
}
```

### IReminderService (Background Job)
```csharp
public interface IReminderService
{
    Task ProcessRemindersAsync(CancellationToken cancellationToken);
    
    // Identify upcoming deadlines
    Task<List<Unit>> GetUnitsNearDeadlineAsync(ReportingCycle cycle, int daysBefore = 3);
    
    // Send reminders
    Task<int> SendDeadlineRemindersAsync(ReportingCycle cycle);
    Task<int> SendOverdueRemindersAsync(ReportingCycle cycle);
    Task<int> SendFollowUpRemindersAsync(ReportingCycle cycle);
}
```

### IActivityLogger
```csharp
public interface IActivityLogger
{
    Task LogActionAsync(
        int reportId, 
        string action,           // "Created", "Submitted", "Approved", etc.
        string? department,      // Null for report-level, "Taleem" for section-level
        string? oldStatus,
        string newStatus,
        int? changedByMemberId,
        string? notes);
    
    Task<List<ReportActivityLog>> GetActivityAsync(int reportId);
}
```

### INotificationService
```csharp
public interface INotificationService
{
    Task NotifyPresidentsAsync(Report report, string type, string message);
    Task NotifyStateGSAsync(Report report, string type, string message);
    Task NotifyNationalAsync(Report report, string type, string message);
    Task NotifyOfficerAsync(Report report, string type, string message);
    
    Task<int> GetUnreadCountAsync(int memberId);
    Task MarkAsReadAsync(int notificationId);
}
```

---

## Database Migrations Plan

### Migration 1: InitialCreate
```csharp
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create all tables
        migrationBuilder.CreateTable("States", ...);
        migrationBuilder.CreateTable("Units", ...);
        migrationBuilder.CreateTable("ReportingCycles", ...);
        migrationBuilder.CreateTable("Reports", ...);
        migrationBuilder.CreateTable("DepartmentReports", ...);
        migrationBuilder.CreateTable("ComplianceChecks", ...);
        migrationBuilder.CreateTable("ReportActivityLogs", ...);
        migrationBuilder.CreateTable("Notifications", ...);
        migrationBuilder.CreateTable("Reminders", ...);
        migrationBuilder.CreateTable("Members", ...);
        
        // Add indexes
        migrationBuilder.CreateIndex("IX_Reports_UnitId_CycleId");
        migrationBuilder.CreateIndex("IX_Reports_Status");
        migrationBuilder.CreateIndex("IX_DepartmentReports_ReportId");
        migrationBuilder.CreateIndex("IX_ReportActivityLog_ReportId");
    }
}
```

### Migration 2: SeedInitialData
```csharp
public partial class SeedInitialData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Seed 37 states
        // Seed ~12 units per state (mock data)
        // Seed current reporting cycle (January 2025)
        // Seed compliance thresholds
    }
}
```

### Migration 3: AddEmailFields
```csharp
// In Phase 4: Add email columns, notification preferences, etc.
```

---

## Dependency Injection Setup

```csharp
// Program.cs

builder.Services
    // Database
    .AddDbContext<ReportingDbContext>(options =>
        options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")))
    
    // Business Services
    .AddScoped<IComplianceEngine, ComplianceEngine>()
    .AddScoped<IReportWorkflow, ReportWorkflow>()
    .AddScoped<IActivityLogger, ActivityLogger>()
    .AddScoped<INotificationService, NotificationService>()
    
    // Background Services
    .AddHostedService<ReminderBackgroundService>()
    
    // API Services
    .AddScoped<IReportApiService, ReportApiService>()
    .AddScoped<IComplianceApiService, ComplianceApiService>()
    .AddScoped<IAnalyticsApiService, AnalyticsApiService>()
    
    // Authentication
    .AddScoped<IAuthService, AmSAAuthService>()  // Real AMSA API
    .AddScoped<IUserContextService, UserContextService>()
    
    // Caching
    .AddMemoryCache()
    .AddStackExchangeRedisCache(options =>
        options.Configuration = Configuration.GetConnectionString("Redis"))
    
    // CORS
    .AddCors(options => options.AddPolicy("BlazorCors", builder =>
        builder.WithOrigins("https://localhost:7001")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials()));
```

---

## Error Handling & Validation

### Exception Types
```csharp
public class ComplianceException : Exception { }
public class WorkflowException : Exception { }          // Invalid state transition
public class AuthorizationException : Exception { }      // User lacks permission
public class EntityNotFoundException : Exception { }      // Report not found
public class ValidationException : Exception { }         // Input validation failed
```

### Global Exception Handler Middleware
```csharp
app.UseExceptionHandler(app => app.Run(async context =>
{
    var feature = context.Features.Get<IExceptionHandlerPathFeature>();
    var exception = feature?.Error;
    
    var response = exception switch
    {
        ComplianceException => new { error = "Compliance check failed", details = exception.Message },
        WorkflowException => new { error = "Invalid workflow state", details = exception.Message },
        AuthorizationException => new { error = "Unauthorized", statusCode = 403 },
        EntityNotFoundException => new { error = "Not found", statusCode = 404 },
        ValidationException => new { error = "Validation failed", details = exception.Message },
        _ => new { error = "Internal server error" }
    };
    
    context.Response.StatusCode = GetStatusCode(exception);
    await context.Response.WriteAsJsonAsync(response);
}));
```

---

## Security Considerations

1. **Authentication:** Real AMSA API (JWT tokens)
2. **Authorization:** Role-based (Officer → President → State → National)
3. **Data:** Encrypt sensitive fields (member emails, phone numbers)
4. **Audit:** Log all changes (ComplianceCheck + ReportActivityLog)
5. **API:** Rate limiting, input validation, CORS

---

## Performance Optimization

1. **Caching:** Cache compliance thresholds, member list, state data
2. **Indexes:** Index on (UnitId, CycleId), Status, CreatedAt
3. **Pagination:** All list endpoints support limit/offset
4. **Async:** All I/O operations async end-to-end
5. **Eager Loading:** Use .Include() to avoid N+1 queries

---

## Summary

**Backend is ready to build:**
- ✅ Database schema designed (11 entities)
- ✅ Report state machine documented
- ✅ API endpoints specified (30+ endpoints)
- ✅ Service layer interfaces defined
- ✅ DI setup clear
- ✅ Error handling strategy
- ✅ Security considerations

**Estimated implementation time: 2-3 days**
- Day 1: EF Core + Migrations
- Day 2: API Controllers + Services
- Day 3: Integration + Testing

Start with core endpoints (Reports CRUD + Workflow) before adding analytics.
