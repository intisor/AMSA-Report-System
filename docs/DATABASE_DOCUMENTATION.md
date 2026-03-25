# Database Documentation Summary

## Overview

The AMSA Reporting System uses **two SQL Server databases**:

1. **AmsaDB** (Existing - AMSA API manages)
   - Contains members, units, states, organizations
   - Read-only access via AMSA API
   
2. **AmsaReportingDB** (New - Reporting System owns)
   - Contains all reporting data
   - Independent schema with app-level FK validation

---

## Database Architecture

### Core Tables (Unit Reports)

#### 1. **ReportingCycles**
Manages monthly reporting windows and deadlines.

```
CycleId (PK)
ReportMonth (UNIQUE, format: 'YYYY-MM')
SubmissionDeadline (DateTime)
ReminderSentAt (nullable)
IsLocked (bit) - Prevents new submissions after deadline
CreatedBy (MemberId from AmsaDB)
CreatedAt, UpdatedAt
```

#### 2. **Reports** (Unit-level)
One report per unit per cycle. Tracks overall submission status through approval chain.

```
ReportId (PK)
CycleId (FK)
UnitId (app-level ref to AmsaDB)
SubmittedBy (MemberId)
Status (Draft → SubmittedToPresident → ApprovedByPresident → 
        SubmittedToState → ApprovedByState → SubmittedToNational → Acknowledged)
PresidentialNote, StateNote, NationalNote
PresidentApprovedBy, PresidentApprovedAt
StateApprovedBy, StateApprovedAt
NationalAcknowledgedBy, NationalAcknowledgedAt
UNIQUE (UnitId, CycleId)
```

**Status Workflow:**
```
CREATED
   ↓
DRAFT (depts filling sections)
   ↓
SUBMITTED_TO_PRESIDENT (all sections submitted)
   ↓ [President approves/rejects]
APPROVED_BY_PRESIDENT ↔ DRAFT (if rejected)
   ↓ [Auto-forward to State]
SUBMITTED_TO_STATE
   ↓ [State GS approves/rejects]
APPROVED_BY_STATE ↔ SUBMITTED_TO_STATE (if rejected)
   ↓ [Auto-forward to National]
SUBMITTED_TO_NATIONAL
   ↓ [National GS acknowledges]
ACKNOWLEDGED (terminal)
```

#### 3. **DepartmentReports**
One row per department per Report (9 total per unit report).

```
DepartmentReportId (PK)
ReportId (FK)
Department (Enum: Taleem, Tabligh, Welfare, Sport, Finance, Health, SecondarySchool, Tajneed, General)
SubmittedBy (MemberId)
Status (Draft → Submitted)
SubmittedAt
UNIQUE (ReportId, Department)
```

### Department-Specific Tables (9 Types)

#### 4. **TaleemReports** (Islamic Education)
```
TaleemReportId (PK)
DepartmentReportId (FK, UNIQUE)
AverageAttendanceCount (int)
TotalMemberCount (int)
AttendanceBelowThresholdReason (nullable)
SessionDurationMinutes (int)
MonthlyTestCount (int)
SessionsOrganized (int)
Notes
IsSessionsCompliant (sessions >= 4)
IsAttendanceCompliant (attendance >= 75%)
```

**Compliance Threshold:** 4+ sessions/month AND 75%+ attendance

#### 5. **TablighReports** (Preaching)
```
TablighReportId (PK)
DepartmentReportId (FK, UNIQUE)
ActivitiesDetail (text)
Purpose (text)
AttendanceCount (int)
Beneficiaries (text)
Notes
HasOnCampusActivity (bit)
IsCompliant (bit) - requires at least 1 on-campus activity
```

**Compliance Threshold:** At least 1 on-campus tabligh exercise

#### 6. **WelfareReports**
```
WelfareReportId (PK)
DepartmentReportId (FK, UNIQUE)
ProgramCount (int)
ParticipantCount (int)
ActivitiesWithDates (text)
BriefReport (text)
Notes
IsCompliant (bit) - requires >= 2 programs
```

**Compliance Threshold:** 2+ activities

#### 7. **SportReports**
```
SportReportId (PK)
DepartmentReportId (FK, UNIQUE)
GamesPlayed (text)
MemberParticipantCount (int)
NonMemberParticipantCount (int)
TotalMemberCount (int)
Notes
IsAttendanceCompliant (bit) - member attendance >= 75%
```

**Compliance Threshold:** 75%+ member participation

#### 8. **FinanceReports**
```
FinanceReportId (PK)
DepartmentReportId (FK, UNIQUE)
DuesCollected (decimal 10,2)
ExpectedDuesAmount (decimal 10,2)
DefaultersReason (nullable)
Notes
CollectionRate (decimal 5,2) - computed: (DuesCollected/ExpectedDues)*100
IsFullyCollected (bit)
```

**Compliance Threshold:** Full collection or flagged with reason

#### 9. **HealthReports**
```
HealthReportId (PK)
DepartmentReportId (FK, UNIQUE)
ActivitiesWithDates (text)
BeneficiaryCount (int)
Notes
```

**Compliance Threshold:** Descriptive only (no threshold)

#### 10. **SecondarySchoolReports**
```
SecondarySchoolReportId (PK)
DepartmentReportId (FK, UNIQUE)
AttendedJamaatMeeting (bit)
ParentSupportEfforts (nullable text)
JambiteParticipation (text)
Notes
```

**Compliance Threshold:** Descriptive only

#### 11. **TajneedReports** (Member Registry)
```
TajneedReportId (PK)
DepartmentReportId (FK, UNIQUE)
HasAccurateTajneed (bit)
ImprovementEfforts (text)
AccuracyPlans (text)
Notes
```

**Compliance Threshold:** Descriptive only

#### 12. **GeneralReports** (Catch-all)
```
GeneralReportId (PK)
DepartmentReportId (FK, UNIQUE)
ChallengesFaced (nullable text)
OtherActivities (nullable text)
Notes
```

**Compliance Threshold:** Descriptive only

### Supporting Tables

#### 13. **ReportAttachments**
```
AttachmentId (PK)
DepartmentReportId (FK)
FileName (nvarchar 255)
FilePath (nvarchar 500)
FileType (e.g. 'image/jpeg')
FileSizeBytes
UploadedBy (MemberId)
UploadedAt
```

#### 14. **Notifications**
```
NotificationId (PK)
RecipientMemberId (FK to AmsaDB)
Type (ReminderDue | ReportSubmitted | ReportApproved | ReportRejected | DeadlineApproaching | CycleLocked)
Title (nvarchar 200)
Message (text)
ReportId (nullable FK)
StateReportId (nullable FK)
CycleId (nullable FK)
IsRead (bit)
CreatedAt
INDEX: (RecipientMemberId, IsRead) for quick "unread" queries
```

#### 15. **ReportActivityLog** (Audit Trail)
```
LogId (PK)
ReportId (FK)
ActionBy (MemberId)
Action (e.g. 'DepartmentSubmitted:Taleem', 'PresidentApproved', 'ForwardedToState')
Notes (nullable)
ActionAt (DateTime)
INDEX: ReportId for audit trail retrieval
```

---

## State Report Tables

#### 16. **StateReports** (One per state per cycle)
```
StateReportId (PK)
CycleId (FK)
StateId (app-level ref to AmsaDB)
SubmittedBy (MemberId)
Status (Draft → SubmittedToPresident → ApprovedByPresident → SubmittedToNational → Acknowledged)
UnitPresidentsAttended (int)
TotalUnitPresidents (int)
UnitPerformanceRating (0-100, nullable)
UnitImprovementPlan (text, nullable)
ChallengesFaced (text, nullable)
NationalSupportNeeded (text, nullable) ← Escalation queue
OtherNotes (text, nullable)
PresidentialNote, NationalNote
PresidentApprovedBy, PresidentApprovedAt
NationalAcknowledgedBy, NationalAcknowledgedAt
UNIQUE (StateId, CycleId)
```

**Computed Property:**
- UnitPresidentAttendanceRate = (UnitPresidentsAttended / TotalUnitPresidents) * 100

#### 17. **StateReportActivities** (Q3: Programs/Activities at state level)
```
ActivityId (PK)
StateReportId (FK)
ActivityTitle (nvarchar 300)
Objectives (text)
Outcomes (text)
AttendanceCount (nullable int)
BeneficiaryCount (nullable int)
ActivityDate (nullable date)
```

#### 18. **StateReportAttachments**
```
AttachmentId (PK)
StateReportId (FK)
FileName, FilePath, FileType
FileSizeBytes
UploadedBy (MemberId)
UploadedAt
```

#### 19. **StateReportActivityLog**
```
LogId (PK)
StateReportId (FK)
ActionBy (MemberId)
Action (e.g. 'Submitted', 'PresidentApproved', 'ForwardedToNational', 'NationalAcknowledged')
Notes (nullable)
ActionAt (DateTime)
```

---

## Key Indexes

```
-- Reports
IX_Reports_CycleId
IX_Reports_UnitId
IX_Reports_Status
IX_Reports_UnitId_CycleId (compound)

-- DepartmentReports
IX_DeptReports_ReportId
IX_DeptReports_Department

-- Notifications
IX_Notifications_Recipient_IsRead (compound) - for "unread" lookups

-- Activity Logs
IX_ActivityLog_ReportId

-- Reporting Cycles
IX_Cycles_ReportMonth

-- State Reports
IX_StateReports_CycleId
IX_StateReports_StateId
IX_StateReports_Status
IX_StateReports_StateId_Cycle (compound)
```

---

## Relationship Diagram

```
ReportingCycle (1)
    ├─ Reports (many) - monthly reports from all units
    ├─ StateReports (many) - monthly reports from all states
    └─ Notifications (many) - cycle-related notifications

Report (1)
    ├─ DepartmentReports (9) - one per department type
    ├─ ReportActivityLog (many) - audit trail
    └─ Notifications (many) - status change notifications

DepartmentReport (1)
    ├─ TaleemReport (0..1)
    ├─ TablighReport (0..1)
    ├─ WelfareReport (0..1)
    ├─ SportReport (0..1)
    ├─ FinanceReport (0..1)
    ├─ HealthReport (0..1)
    ├─ SecondarySchoolReport (0..1)
    ├─ TajneedReport (0..1)
    ├─ GeneralReport (0..1)
    └─ ReportAttachments (many)

StateReport (1)
    ├─ StateReportActivities (many) - Q3 programs
    ├─ StateReportAttachments (many)
    └─ StateReportActivityLog (many)
```

---

## EF Core Configuration

### DbContext DbSets

```csharp
public DbSet<ReportingCycle> ReportingCycles { get; set; }
public DbSet<Report> Reports { get; set; }
public DbSet<DepartmentReport> DepartmentReports { get; set; }
public DbSet<TaleemReport> TaleemReports { get; set; }
public DbSet<TablighReport> TablighReports { get; set; }
public DbSet<WelfareReport> WelfareReports { get; set; }
public DbSet<SportReport> SportReports { get; set; }
public DbSet<FinanceReport> FinanceReports { get; set; }
public DbSet<HealthReport> HealthReports { get; set; }
public DbSet<SecondarySchoolReport> SecondarySchoolReports { get; set; }
public DbSet<TajneedReport> TajneedReports { get; set; }
public DbSet<GeneralReport> GeneralReports { get; set; }
public DbSet<ReportAttachment> ReportAttachments { get; set; }
public DbSet<Notification> Notifications { get; set; }
public DbSet<ReportActivityLog> ReportActivityLogs { get; set; }

// State Reports
public DbSet<StateReport> StateReports { get; set; }
public DbSet<StateReportActivity> StateReportActivities { get; set; }
public DbSet<StateReportAttachment> StateReportAttachments { get; set; }
public DbSet<StateReportActivityLog> StateReportActivityLogs { get; set; }
```

### Key Fluent Configurations

- **Unique Constraints:**
  - ReportingCycle: ReportMonth
  - Report: (UnitId, CycleId)
  - DepartmentReport: (ReportId, Department)
  - StateReport: (StateId, CycleId)

- **Check Constraints:**
  - Report.Status (enum validation)
  - DepartmentReport.Status (Draft/Submitted)
  - DepartmentReport.Department (9 valid types)
  - StateReport.Status (state-specific statuses)
  - StateReport.UnitPerformanceRating (0-100)
  - Notification.Type (notification types)

- **One-to-One Relationships:**
  - DepartmentReport → TaleemReport (and 8 others)
  - All with cascading delete enabled

- **One-to-Many Relationships:**
  - ReportingCycle → Reports, StateReports
  - Report → DepartmentReports, ReportActivityLog
  - DepartmentReport → ReportAttachments
  - StateReport → StateReportActivities, StateReportAttachments, StateReportActivityLog

---

## Cross-Database References

### Reference Strategy

Since ReportingDB and AmsaDB are separate, cross-DB foreign keys are **not enforced at the SQL level**. Instead:

1. **Application-Level Validation:**
   - Before creating a Report, call AMSA API to validate UnitId exists
   - Before accepting a submission, validate SubmittedBy MemberId is valid member of that unit

2. **Display-Time Lookups:**
   - Unit and State names are fetched from AMSA API (not stored in ReportingDB)
   - Ensures org structure changes are immediately reflected

3. **Stored Values:**
   - UnitId, StateId, MemberId stored as simple integers
   - No FK enforcement; app code ensures referential integrity

---

## Compliance Engine Integration

The **ComplianceEngine** evaluates submissions on-the-fly and sets boolean flags:

```csharp
public class ComplianceEngine
{
    public void Evaluate(TaleemReport r)
    {
        r.IsSessionsCompliant = r.SessionsOrganized >= 4;
        r.IsAttendanceCompliant = r.TotalMemberCount > 0 &&
            (double)r.AverageAttendanceCount / r.TotalMemberCount * 100 >= 75;
    }
    
    public void Evaluate(TablighReport r)
    {
        r.IsCompliant = r.HasOnCampusActivity;
    }
    
    // ... etc for other departments
}
```

**Important:** Compliance flags do NOT block submission — they surface issues for review by Unit Presidents and State GS.

---

## Data Migration Path

### Phase 1 Setup:
1. Create AmsaReportingDB (or add to existing SQL Server instance)
2. Run AmsaReporting_Schema.sql
3. Apply EF Core migration

### Phase 2 (State Reports):
1. Run AmsaReporting_StateReports_Schema.sql
2. Add StateReport DbSets to ReportingDbContext
3. Apply EF Core migration

### Testing:
- Seed a ReportingCycle for testing
- Test state machine transitions
- Verify audit trail logging

---

## Performance Considerations

- **Indexes:** Created on all foreign keys, frequently filtered columns, and search paths
- **Indexes on Notifications:** (RecipientMemberId, IsRead) for fast "unread" queries
- **No N+1 Queries:** Use `.Include()` for navigation properties
- **Audit Trail:** Append-only; no updates to ReportActivityLog

---

## Files Reference

- **Schema:** `docs/AmsaReporting_Schema.sql` (unit + state report schema in separate file)
- **State Schema:** `docs/AmsaReporting_StateReports_Schema.sql`
- **EF Models:** `docs/AmsaReporting_EFCore_Models.cs`
- **State Models:** `docs/AmsaReporting_StateReports_EFCore.cs`
- **Complete Models:** `docs/AmsaReporting_CompleteModels.cs`
- **Complete Schema:** `docs/AmsaReporting_CompleteSchema.sql`

