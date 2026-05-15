# AMSA System - State Management & Data Flow Reference

## Quick Reference: How State Flows Through the System

### 1. State Entry Points (Where Changes Start)

```
USER ACTIONS:
├─ Fill form in DepartmentOfficerDashboard
│  └─ Trigger: CurrentUserReportService.SaveDepartmentJsonAsync()
│     └─ Flow: JSON → Database → DepartmentReport entity
│
├─ Click "Submit to President" in UnitLeadershipDashboard
│  └─ Trigger: CurrentUserReportService.SubmitReportToPresidentAsync()
│     └─ Flow: Auth check → Status transition → Database
│
├─ Click "Approve" in UnitLeadershipDashboard
│  └─ Trigger: CurrentUserReportService.ApproveAtUnitAsync()
│     └─ Flow: Auth check → Status transition → Database
│
├─ Fill state form in StateLeadershipDashboard
│  └─ Trigger: CurrentUserReportService.SaveMyStateReportAsync()
│     └─ Flow: Commentary saved → Aggregation runs → Database
│
└─ Click "Acknowledge" in NationalLeadershipDashboard
   └─ Trigger: CurrentUserReportService.AcknowledgeAtNationalAsync()
      └─ Flow: Auth check → Status transition → Database
```

### 2. State Persistence (How Data Gets Saved)

**GOLDEN RULE: Database is the source of truth**

```
┌─────────────────────────────────────────────────────────────┐
│ COMPONENT (Blazor)                                          │
│ User fills form, clicks save                                │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ SERVICE LAYER                                               │
│ 1. Extract current user context                            │
│ 2. Authorization check (throw if unauthorized)             │
│ 3. Load entity from database                               │
│ 4. Validate preconditions (throw if invalid)               │
│ 5. Mutate entity in memory                                 │
│ 6. Add audit log entry                                     │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ ENTITY FRAMEWORK CORE                                       │
│ Entities marked as Modified/Added in ChangeTracker         │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
        ┌─────────────────────┐
        │ SaveChangesAsync() │  ← ATOMIC TRANSACTION BEGINS
        │ (generates SQL)     │
        └─────────┬───────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────────┐
│ SQL SERVER                                                  │
│ BEGIN TRANSACTION                                           │
│   UPDATE Reports SET Status='SubmittedToPresident' ...     │
│   INSERT INTO ReportActivityLog VALUES (...)               │
│ COMMIT                                                      │
└─────────────────────────────────────────────────────────────┘

IF ERROR: ROLLBACK (all-or-nothing)
IF SUCCESS: Return updated entity to component
```

### 3. State Transitions (Report Status Machine)

```
Current Status → Method Call → New Status → Audit Log

Draft
  ├─ SubmitReportToPresidentAsync()
  │  └─ SubmittedToPresident ✓
  │     └─ Log: "ReportSubmittedToPresident"
  │
  └─ Called from: Unit member submits ≥1 department

SubmittedToPresident
  ├─ ApproveByUnitLeadershipAsync()
  │  └─ SubmittedToState ✓
  │     └─ Log: "ApprovedByUnitLeadership"
  │
  ├─ RejectByUnitLeadershipAsync()
  │  └─ RejectedByPresident ✓
  │     └─ Log: "RejectedByUnitLeadership"
  │     └─ (back to Draft via user re-submit)
  │
  └─ Called from: Unit president reviews

SubmittedToState
  ├─ ApproveByStateLeadershipAsync()
  │  └─ SubmittedToNational ✓
  │     └─ Log: "ApprovedByStateLeadership"
  │     └─ (triggers aggregation)
  │
  ├─ RejectByStateLeadershipAsync()
  │  └─ RejectedByState ✓
  │     └─ Log: "RejectedByStateLeadership"
  │     └─ (back to Draft via unit re-submit)
  │
  └─ Called from: State GS reviews

SubmittedToNational
  ├─ AcknowledgeByNationalAsync()
  │  └─ Acknowledged ✓ [FINAL]
  │     └─ Log: "AcknowledgedByNational"
  │     └─ No further transitions allowed
  │
  └─ Called from: National leadership acknowledges
```

### 4. Aggregation Trigger (State Report Auto-Aggregation)

```
State GS clicks "Save & Submit State Report"
│
▼
CurrentUserReportService.SaveMyStateReportAsync(stateReportId, form, markSubmitted=true)
│
▼
UnifiedReportService.SaveStateReportAsync(actor, stateReportId, form, markSubmitted=true)
│
├─ Load StateReport entity
├─ Update: UnitImprovementPlan, ChallengesFaced, etc.
│
├─ Call: RecomputeStateAggregateAsync(stateReport)
│  │
│  ├─ Query all Reports for this state + cycle
│  │  └─ Find: UnitsAttendedTo (count where status ≥ SubmittedToState)
│  │  └─ Find: TotalUnitReportsCount (count of all unit reports)
│  │
│  ├─ Query all DepartmentReports (IsSubmitted=true) for all units
│  │  └─ Group by Department
│  │
│  ├─ For each Department group:
│  │  ├─ Sum: AttendanceCount
│  │  ├─ Sum: BeneficiaryCount
│  │  └─ Create StateReportProgram(IsAutoAggregated=true)
│  │
│  ├─ Delete old auto-aggregated programs (preserve manual ones)
│  │
│  └─ Mark: IsAggregated=true, LastAggregatedAt=now
│
├─ If markSubmitted=true:
│  └─ Update: StateReport.Status = SubmittedToNational
│
├─ Add audit log entry
│
└─ SaveChangesAsync() ← ATOMIC (all-or-nothing)
   │
   ▼
   StateReport now has:
   ├─ Leadership commentary (manual)
   ├─ Aggregated programs (auto)
   ├─ Performance metrics (calculated)
   └─ Timestamp (LastAggregatedAt)
```

### 5. Composite DTO Creation (National Dashboard Data Load)

```
Component: NationalLeadershipDashboard
│
▼
CurrentUserReportService.GetNationalReportsWithStateContextAsync(cycleId)
│
▼
UnifiedReportService.GetNationalReportsWithStateContextAsync(actor, cycleId)
│
├─ Auth check: IsNationalLeadership(actor) ✓
│
├─ Load all Reports for cycle:
│  └─ SELECT Reports WHERE CycleId = @cycleId
│  └─ Include: Cycle, DepartmentReports, ActivityLogs
│
├─ For EACH Report:
│  │
│  ├─ Load StateReport for same state + cycle:
│  │  └─ SELECT StateReport WHERE StateId = Report.StateId AND CycleId = @cycleId
│  │  └─ Include: Programs
│  │
│  ├─ Map Report → ReportDepartmentView[] (from DepartmentReports)
│  │
│  ├─ Map StateReport → StateReportContext (if not null):
│  │  ├─ StateReportId
│  │  ├─ UnitsAttendedTo, TotalUnitReportsCount, UnitPerformanceRating
│  │  ├─ Commentary fields
│  │  ├─ Approval chain
│  │  ├─ Aggregation metadata
│  │  └─ Programs → StateReportProgramView[]
│  │
│  ├─ Map ActivityLogs → ReportActivityView[]
│  │
│  └─ Create ReportWithStateContext(
│       ReportId, UnitId, StateId, ...,
│       StateContext = stateReport is null ? null : new StateReportContext(...),
│       Departments = [...],
│       ActivityLogs = [...]
│     )
│
└─ Return List<ReportWithStateContext>
   │
   ▼
   Component caches list in local state
   │
   ▼
   Component calls WarmReportNamesAsync():
   ├─ Extract unique UnitIds
   ├─ Call DirectoryLookupCache.GetUnitNameAsync(unitId) for each
   │  └─ Lookup: scoped cache → AMSA API (on miss)
   ├─ Extract unique StateIds
   └─ Call DirectoryLookupCache.GetStateNameAsync(stateId) for each
   │
   ▼
   Component renders table:
   ├─ Row 1: Unit A, State 1, Status=SubmittedToNational, Units Attended=5/8, [Acknowledge]
   ├─ Row 2: Unit B, State 1, Status=SubmittedToNational, Units Attended=5/8, [Acknowledge]
   ├─ Row 3: Unit C, State 2, Status=SubmittedToNational, Units Attended=3/4, [Acknowledge]
   └─ ...
```

---

## Reference: Service Call Signatures

### CurrentUserReportService (Public API)

```csharp
// Cycle Management
Task<ReportingCycle> EnsureActiveCycleAsync(CancellationToken ct = default)

// Department Report Operations
Task<DepartmentReport?> GetDepartmentAsync(int reportId, DepartmentType department, CancellationToken ct = default)
Task<DepartmentReport> SaveDepartmentJsonAsync(int reportId, DepartmentType department, string reportDataJson, bool markSubmitted, CancellationToken ct = default)

// Report Retrieval
Task<List<ReportRetrievalSummary>> GetCurrentUserReportHistoryAsync(CancellationToken ct = default)
Task<ReportRetrievalDetails?> GetReportDetailsAsync(int reportId, CancellationToken ct = default)

// Unit-Level Operations
bool CanEditOwnUnitDepartment(DepartmentType department)
bool CanInitiateCurrentUserReportSubmission()
Task<Report> SubmitReportToPresidentAsync(int reportId, string? notes = null, CancellationToken ct = default)

// Unit/State/National Report Views
Task<List<Report>> GetMyUnitReportsAsync(int? cycleId = null, CancellationToken ct = default)
Task<List<Report>> GetMyStateReportsAsync(int? cycleId = null, CancellationToken ct = default)
Task<List<Report>> GetStateUnitReportsAsync(int? cycleId = null, CancellationToken ct = default)
Task<StateReport?> GetMyStateReportAsync(int cycleId, CancellationToken ct = default)
Task<StateReport> CreateMyStateReportAsync(int cycleId, CancellationToken ct = default)
Task<StateReport> SaveMyStateReportAsync(int stateReportId, StateReportForm form, bool markSubmitted, CancellationToken ct = default)
Task<List<Report>> GetNationalReportsAsync(int? cycleId = null, CancellationToken ct = default)

// NEW: Composite view for National Dashboard
Task<List<ReportWithStateContext>> GetNationalReportsWithStateContextAsync(int? cycleId = null, CancellationToken ct = default)

// Leadership Actions
Task<Report> ApproveAtUnitAsync(int reportId, string? notes = null, CancellationToken ct = default)
Task<Report> RejectAtUnitAsync(int reportId, string? notes = null, CancellationToken ct = default)
Task<Report> ApproveAtStateAsync(int reportId, string? notes = null, CancellationToken ct = default)
Task<Report> RejectAtStateAsync(int reportId, string? notes = null, CancellationToken ct = default)
Task<Report> AcknowledgeAtNationalAsync(int reportId, string? notes = null, CancellationToken ct = default)
```

### Authorization: ReportAccessService

```csharp
bool IsNationalLeadership(AuthContext actor)
bool IsStateLeadership(AuthContext actor)
bool IsUnitLeadership(AuthContext actor)
bool CanEditDepartment(AuthContext actor, int unitId, int stateId, DepartmentType department)
bool CanInitiateReportSubmission(AuthContext actor, int unitId, int stateId)
bool CanReviewAtUnitLevel(AuthContext actor, int unitId)
bool CanReviewAtStateLevel(AuthContext actor, int stateId)
```

---

## Reference: Entity Relationships & Cascading

### Delete Cascade Rules

```
ReportingCycle --[Restrict]--> Report
               └--[Restrict]--> StateReport
  (Don't delete cycles; they're historical records)

Report --[Cascade]--> DepartmentReport
     └--[Cascade]--> ReportActivityLog
  (Deleting a report deletes all its departments and activity logs)

StateReport --[Cascade]--> StateReportProgram
  (Deleting state report deletes its programs)
```

### Foreign Key Constraints

```
Report.CycleId → ReportingCycle.Id [NOT NULL]
Report.UnitId [NOT NULL]
Report.StateId [NOT NULL]

DepartmentReport.ReportId → Report.Id [NOT NULL] [CASCADE]
DepartmentReport.CycleId (nullable, denormalized for analytics)
DepartmentReport.Department [NOT NULL]

StateReport.StateId [NOT NULL]
StateReport.CycleId → ReportingCycle.Id [NOT NULL]

StateReportProgram.StateReportId → StateReport.Id [NOT NULL] [CASCADE]

ReportActivityLog.ReportId → Report.Id [NOT NULL] [CASCADE]
ReportActivityLog.ActionByMemberId [NOT NULL]
```

---

## Reference: Unique Constraints & Indices

### Unique Constraints (Business Rules)

```
ReportingCycle.CycleMonth UNIQUE
  └─ One cycle per calendar month

Report: (UnitId, CycleId) UNIQUE
  └─ One report per unit per cycle

DepartmentReport: (ReportId, Department) UNIQUE
  └─ One department entry per report

StateReport: (StateId, CycleId) UNIQUE
  └─ One state report per state per cycle
```

### Indices (Performance)

```
Report:
  └─ Index: Status (filter by status)
  └─ Index: UnitId (lookup reports by unit)
  └─ Index: StateId (lookup reports by state)
  └─ Index: CycleId (lookup reports by cycle)

DepartmentReport:
  └─ Index: ReportId (lookup departments by report)
  └─ Index: CycleId (analytics queries)

StateReport:
  └─ Index: Status (filter submissions)
  └─ Index: StateId (lookup by state)
  └─ Index: CycleId (lookup by cycle)

StateReportProgram:
  └─ Index: StateReportId (lookup programs)

ReportActivityLog:
  └─ Index: ReportId (fetch activity log for report)
```

---

## Reference: Cycle Lifecycle

### Monthly Cycle Determinism

```
EnsureActiveCycleAsync() [Called at app startup or on demand]
│
├─ Get today's date: DateTime.UtcNow.Date
├─ Extract month start & month end
│
├─ Query: SELECT * FROM ReportingCycles WHERE StartDate = @monthStart AND EndDate = @monthEnd
│
├─ If found: Return existing cycle (idempotent)
│
└─ If not found:
   ├─ Create new ReportingCycle:
   │  ├─ CycleMonth = "January 2025"
   │  ├─ StartDate = 2025-01-01
   │  ├─ EndDate = 2025-01-31
   │  ├─ SubmissionDeadline = 2025-01-31
   │  ├─ IsLocked = false
   │  └─ CreatedAt = now
   │
   └─ SaveChangesAsync() → Persist
```

### Cycle Auto-Locking

```
EnsureCycleOpenForEditsAsync(cycleId) [Called before every edit]
│
├─ Load ReportingCycle
├─ Check: DateTime.UtcNow.Date > cycle.SubmissionDeadline.Date
│
├─ If past deadline AND NOT locked:
│  ├─ Update: cycle.IsLocked = true
│  ├─ SaveChangesAsync()
│  └─ Proceed to throw
│
├─ If locked OR past deadline:
│  └─ Throw InvalidOperationException("This reporting cycle is locked...")
│
└─ Else: Allow edit to proceed
```

---

## Reference: Validation Checklist

### Before State Transitions

```
SubmitReportToPresidentAsync():
  ├─ ✓ Authorization: CanInitiateReportSubmission(actor, unitId, stateId)
  ├─ ✓ Cycle open: EnsureCycleOpenForEditsAsync(cycleId)
  ├─ ✓ Status valid: Report.Status = Draft OR RejectedByPresident
  └─ ✓ At least 1 dept submitted: DepartmentReports.Any(d => d.IsSubmitted)

ApproveByUnitLeadershipAsync():
  ├─ ✓ Authorization: CanReviewAtUnitLevel(actor, unitId)
  ├─ ✓ Status valid: Report.Status = SubmittedToPresident OR Draft OR RejectedByPresident
  └─ ✓ At least 1 dept submitted: DepartmentReports.Any(d => d.IsSubmitted)

ApproveByStateLeadershipAsync():
  ├─ ✓ Authorization: CanReviewAtStateLevel(actor, stateId)
  └─ ✓ Status valid: Report.Status = SubmittedToState

RejectByStateLeadershipAsync():
  ├─ ✓ Authorization: CanReviewAtStateLevel(actor, stateId)
  └─ ✓ Status valid: Report.Status = SubmittedToState

AcknowledgeByNationalAsync():
  ├─ ✓ Authorization: IsNationalLeadership(actor)
  └─ ✓ Status valid: Report.Status = SubmittedToNational

SaveStateReportAsync():
  ├─ ✓ Authorization: CanReviewAtStateLevel(actor, stateId)
  ├─ ✓ Cycle open: EnsureCycleOpenForEditsAsync(cycleId)
  ├─ ✓ Status not final: StateReport.Status ≠ SubmittedToNational AND ≠ Acknowledged
  └─ ✓ Form valid: UnitPresidentsAttended ≤ TotalUnitPresidents, etc.
```

---

## Reference: Error Handling

### Exceptions Thrown by Services

```
UnauthorizedAccessException:
  └─ Authorization check failed
  └─ Thrown by: ReportAccessService.CanXxx() calls
  └─ Handle: Show 403 Forbidden UI

InvalidOperationException:
  └─ Business logic violation
  └─ Thrown by: Status transition rules, cycle locking, form validation
  └─ Examples:
     ├─ "Report {reportId} not found."
     ├─ "Report must be in {status} state for {action}."
     ├─ "At least one department report must be submitted..."
     ├─ "This reporting cycle is locked. Submission deadline was..."
     ├─ "Attendance values cannot be negative."
     └─ "This state report has already been submitted and is read-only."

ArgumentException:
  └─ Invalid input parameters
  └─ Thrown by: JSON validation, null checks
  └─ Examples:
     ├─ "Report data JSON cannot be empty."
     ├─ "Invalid JSON format in report data."

ArgumentNullException:
  └─ Null parameter when required
  └─ Thrown by: ArgumentNullException.ThrowIfNull()
```

### Component Error Handling Pattern

```csharp
private async Task Acknowledge(int reportId)
{
    try
    {
        var updated = await CurrentUserReportService.AcknowledgeAtNationalAsync(reportId, notes);
        ReplaceReport(updated);
        SetMessage($"Report #{reportId} acknowledged by national leadership.", true);
    }
    catch (Exception ex)
    {
        SetMessage(ex.Message, false);  // Display error to user
    }
}

private void SetMessage(string text, bool success)
{
    Message = text;
    MessageClass = success ? "alert-success" : "alert-danger";
}
```

---

## Reference: Audit Trail Structure

### ReportActivityLog Records

```csharp
new ReportActivityLog
{
    ReportId = reportId,              // Which report this action is for
    ActionByMemberId = actor.MemberId, // Who performed the action
    Action = "DepartmentReportSubmitted", // What action type
    ActionAt = DateTime.UtcNow,       // When it happened (UTC)
    Notes = notes                     // Optional: rejection reason, feedback, etc.
}
```

### Action Types (Strings)

```
Department-Level:
  └─ "DepartmentReportSaved"
  └─ "DepartmentReportSubmitted"

Report-Level:
  └─ "ReportCreated"
  └─ "ReportSubmittedToPresident"
  └─ "ApprovedByUnitLeadership"
  └─ "RejectedByUnitLeadership"
  └─ "ApprovedByStateLeadership"
  └─ "RejectedByStateLeadership"
  └─ "AcknowledgedByNational"
```

### Querying Audit Trail

```csharp
// Get all actions for a specific report
var actions = report.ActivityLogs
    .OrderByDescending(log => log.ActionAt)
    .ToList();

// Get actions by a specific member
var memberActions = report.ActivityLogs
    .Where(log => log.ActionByMemberId == memberId)
    .ToList();

// Get all rejections (for debugging why report is in RejectedByPresident state)
var rejections = report.ActivityLogs
    .Where(log => log.Action.Contains("Rejected"))
    .OrderByDescending(log => log.ActionAt)
    .ToList();
```

---

**END OF STATE MANAGEMENT & DATA FLOW REFERENCE**

Use this document to understand:
1. Where state enters the system (user actions)
2. How state is persisted (database as truth)
3. What transitions are allowed (state machine rules)
4. How aggregation works (StateReport creation)
5. How composite DTOs are built (National Dashboard)
6. Error handling patterns (exceptions & UI feedback)
7. Audit trail structure (immutable history)
