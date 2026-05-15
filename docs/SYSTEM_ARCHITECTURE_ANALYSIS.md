# AMSA Reporting System - Deep Architectural Analysis & State Management

## Table of Contents
1. [System Overview](#system-overview)
2. [Organizational Hierarchy](#organizational-hierarchy)
3. [Core Architecture Layers](#core-architecture-layers)
4. [Data Model Architecture](#data-model-architecture)
5. [Report Workflow & State Machine](#report-workflow--state-machine)
6. [State Management Deep Dive](#state-management-deep-dive)
7. [Module Integration Map](#module-integration-map)
8. [Data Flow Patterns](#data-flow-patterns)
9. [Authorization & Access Control](#authorization--access-control)
10. [Composite Views & DTO Architecture](#composite-views--dto-architecture)
11. [Key Design Decisions](#key-design-decisions)

---

## System Overview

**AMSA Reporting System** is a multilevel hierarchical reporting platform for the AMSA organization, enabling monthly compliance reporting across 9 departments at three organizational levels (Unit, State, National).

### Key Characteristics
- **Architecture**: Blazor Server + WebAssembly (dual-mode rendering)
- **Database**: SQL Server with EF Core
- **Framework**: .NET 10
- **API Integration**: AMSA REST API for member/unit/state data
- **State Model**: Complex multi-level workflow with approval chains
- **Authorization**: Role-based access control (RBAC) with hierarchical department targeting

---

## Organizational Hierarchy

```
NATIONAL LEADERSHIP
├── State GS (General Secretary)
│   ├── Unit President (one per unit in state)
│   │   ├── Department Officers (Taleem, Tabligh, Welfare, Sport, Finance, Health, etc.)
│   │   └── General Secretary (unit-level)
│   └── Department Officers (State-level, e.g., State Taleem Secretary)
└── VP Positions (Admin, North, South-Southwest, Southwest) - National only

HIERARCHY LEVELS:
- Unit Level (LevelType.Unit): Department officers, unit president
- State Level (LevelType.State): State GS, state department heads
- National Level (LevelType.National): National leadership, VPs
```

---

## Core Architecture Layers

### 1. **Presentation Layer (Blazor Components)**

Located in: `AMSAReportingSystem\Components\`

**Role-Based Dashboard Routing:**
- `NationalLeadershipDashboard.razor` → National leadership view
- `StateLeadershipDashboard.razor` → State GS/department heads
- `UnitLeadershipDashboard.razor` → Unit presidents
- `DepartmentOfficerDashboard.razor` → Regular department officers
- `Dashboard.razor` → Router component (routes to appropriate dashboard)

**Key Pattern:**
- Components use `CurrentUserReportService` (scoped service wrapping `UnifiedReportService`)
- Automatic user context injection via `AMSAAuthStateProvider`
- No need to manually pass `AuthContext` in component parameters

### 2. **Service Layer**

**Core Services:**

#### `UnifiedReportService` (Business Logic)
- **Scope**: Scoped (per request in Blazor Server)
- **Responsibility**: All report operations with authorization checks
- **Operations Organized by Domain**:
  - Department Report Operations (save, retrieve)
  - State Report Operations (aggregation, lifecycle)
  - Report Lifecycle & Submission (workflow state transitions)
  - Reporting Cycle Management (monthly cycle creation/locking)
  - Utilities (validation, aggregation)

#### `CurrentUserReportService` (Convenience Wrapper)
- **Scope**: Scoped
- **Responsibility**: Wraps `UnifiedReportService` with automatic current user extraction
- **Pattern**: Reduces boilerplate in components
- **Implementation**: Uses `AMSAAuthStateProvider` + `ExecuteAsCurrentUserAsync()`

#### `ReportAccessService` (Authorization)
- **Scope**: Can be instantiated per-call
- **Methods**:
  - `CanEditDepartment(actor, unitId, stateId, department)` → Department-level RBAC
  - `CanInitiateReportSubmission(actor, unitId, stateId)` → Report creation authorization
  - `CanReviewAtUnitLevel(actor, unitId)` → Unit-level approval authority
  - `CanReviewAtStateLevel(actor, stateId)` → State-level approval authority
  - `IsNationalLeadership(actor)` → National-level access
- **Authorization Model**:
  - Hierarchical: National > State > Unit
  - Department-specific: Officers can edit their department's reports
  - Sudo access: Any leadership role grants broad permissions

#### `AmsaAuthService` (Authentication)
- **Scope**: Scoped
- **Responsibility**: 
  - MKAN-based member lookup
  - JWT token generation via AMSA API
  - Role parsing (e.g., "Taleem:Unit" → DepartmentName + LevelType)

#### `AMSAAuthStateProvider` (State Provider)
- **Scope**: Scoped
- **Responsibility**: 
  - Cascading authentication state for Blazor
  - Current user context retrieval
  - Token refresh management

#### `AmsaDirectoryLookupCache` (Caching)
- **Scope**: Scoped
- **Responsibility**: 
  - In-memory cache for unit/state names
  - Reduces repeated API calls during rendering
  - Used by dashboard components for display names

#### `AmsaApiClient` (External Integration)
- **Scope**: Transient (registered as typed HttpClient)
- **Responsibility**: 
  - REST API calls to AMSA backend
  - Member/unit/state lookups
  - Token generation
- **Token Caching**: `AmsaTokenCache` (singleton) prevents token regeneration

### 3. **Data Access Layer (Entity Framework Core)**

**DbContext**: `AMSAReportingDbContext`

**DbSets:**
```csharp
public DbSet<ReportingCycle> ReportingCycles { get; set; }
public DbSet<Report> Reports { get; set; }
public DbSet<DepartmentReport> DepartmentReports { get; set; }
public DbSet<StateReport> StateReports { get; set; }
public DbSet<StateReportProgram> StateReportPrograms { get; set; }
public DbSet<ReportActivityLog> ReportActivityLogs { get; set; }
```

**Indexing Strategy:**
- Composite unique indexes on (UnitId, CycleId) for Reports
- Composite unique indexes on (ReportId, Department) for DepartmentReports
- Composite unique indexes on (StateId, CycleId) for StateReports
- Status-based indexes for workflow queries

---

## Data Model Architecture

### Core Entities Relationships

```
ReportingCycle (1) ----< (many) Report
                   └----< (many) StateReport

Report (1) ----< (many) DepartmentReport
      (1) ----< (many) ReportActivityLog

StateReport (1) ----< (many) StateReportProgram
```

### Entity Responsibilities

#### `ReportingCycle`
- **Purpose**: Encapsulates a monthly reporting period
- **Key Fields**: StartDate, EndDate, SubmissionDeadline, IsLocked
- **Lifecycle**:
  1. Created deterministically for calendar month (via `EnsureActiveCycleAsync`)
  2. Submission deadline = last day of month
  3. Auto-locked if deadline passes
- **Seeding**: January 2025 cycle created in migrations

#### `Report` (Unit Report)
- **Purpose**: Aggregates all 9 department submissions for a unit in a cycle
- **Unique Constraint**: (UnitId, CycleId) - one report per unit per cycle
- **Status Machine**: See [Report Workflow](#report-workflow--state-machine)
- **Tracking**: Submission/approval timestamps + member IDs at each stage
- **Notes Fields**: Presidential, State, National notes at each approval level

#### `DepartmentReport`
- **Purpose**: Holds submission status and JSON data for one department in one unit report
- **Unique Constraint**: (ReportId, Department) - one row per department per unit report
- **Hybrid Model**: 
  - JSON payload in `ReportData` (flexible schema)
  - Extracted analytics fields (denormalized): SessionsOrganized, AttendanceCount, etc.
- **Submission Tracking**: IsSubmitted, SubmittedAt, SubmittedByMemberId
- **9 Department Types**: Taleem, Tabligh, Welfare, Sport, Finance, Health, SecondarySchool, Tajneed, General

#### `StateReport`
- **Purpose**: State-level aggregation and leadership commentary
- **Unique Constraint**: (StateId, CycleId) - one per state per cycle
- **Content**: Leadership commentary (UnitImprovementPlan, ChallengesFaced, NationalSupportNeeded, OtherNotes)
- **Metrics**: UnitsAttendedTo, TotalUnitReportsCount, UnitPerformanceRating (0-100)
- **Computed Property**: UnitPresidentAttendanceRate = (UnitsAttendedTo / TotalUnitReportsCount) * 100
- **Approval Chain**: Presidential approval, then National acknowledgment

#### `StateReportProgram`
- **Purpose**: Auto-aggregated programs from submitted department reports
- **Source Flag**: `IsAutoAggregated` (true when created by aggregation engine)
- **Preservation Logic**: 
  - Only auto-aggregated rows are removed during re-aggregation
  - Manual entries preserved across aggregations
- **Metrics**: TotalAttendance (sum), TotalBeneficiaries (sum)

#### `ReportActivityLog`
- **Purpose**: Audit trail for all report workflow actions
- **Stored Events**: 
  - DepartmentReportSaved, DepartmentReportSubmitted
  - ReportCreated, ReportSubmittedToPresident
  - ApprovedByUnitLeadership, RejectedByUnitLeadership
  - ApprovedByStateLeadership, RejectedByStateLeadership
  - AcknowledgedByNational
- **Retention**: Permanent record of all state transitions

---

## Report Workflow & State Machine

### Report Status Enum

```csharp
public enum ReportStatus
{
    Draft = 0,                  // Initial state
    SubmittedToPresident = 1,   // After unit submits (to unit president)
    RejectedByPresident = 2,    // Unit president rejects → back to Draft
    ApprovedByPresident = 3,    // [Deprecated?] Not used in transitions
    SubmittedToState = 4,       // Unit president approves → to state GS
    RejectedByState = 5,        // State GS rejects → back to Draft
    ApprovedByState = 6,        // [Deprecated?] Not used in transitions
    SubmittedToNational = 7,    // State GS approves → to national leadership
    Acknowledged = 8            // National acknowledges → final state
}
```

### Workflow State Machine

```
                              ┌─────────────────┐
                              │      DRAFT      │◄──────────┐
                              └────────┬────────┘           │
                                       │                    │
                    ┌──────────────────►Submit◄──────────────┤
                    │                  to Pres.             │
                    │                   │                   │
                    │                   ▼                   │
                    │        ┌──────────────────────┐       │
                    │        │ SubmittedToPresident │       │
                    │        └──────┬───────────┬───┘       │
                    │               │           │           │
         Reject     │      Approve  │ Reject    │           │
            ◄───────┤               ▼           ▼           │
            │       │        ┌──────────────────────┐       │
            │       │        │ SubmittedToState     │       │
            │       └───────►│ (via ApproveByUnit)  │       │
            │                └──────┬────────┬──────┘       │
            │                       │        │              │
            │              Approve  │ Reject │              │
            │                       ▼        ▼              │
            │                ┌──────────────────────┐       │
            │                │ SubmittedToNational  │       │
            │                │(via ApproveByState)  │       │
            │                └──────┬────────┬──────┘       │
            │                       │        │              │
            │              Acknowledge Reject│              │
            │                       ▼        ▼              │
            │                ┌──────────────────────┐       │
            │                │   Acknowledged      │       │
            │                │   (FINAL)           │       │
            │                └─────────────────────┘       │
            │                                              │
            └──────────────────────────────────────────────┘
                        RejectedByState/
                        RejectedByPresident
                        (back to Draft)
```

### State Transitions (Methods in UnifiedReportService)

| Method | From States | To State | Role | Constraint |
|--------|------------|----------|------|-----------|
| `SubmitReportToPresidentAsync` | Draft, RejectedByPresident | SubmittedToPresident | Unit member | ≥1 dept submitted |
| `ApproveByUnitLeadershipAsync` | SubmittedToPresident, Draft, RejectedByPresident | SubmittedToState | Unit president | ≥1 dept submitted |
| `RejectByUnitLeadershipAsync` | SubmittedToPresident, Draft | RejectedByPresident | Unit president | - |
| `ApproveByStateLeadershipAsync` | SubmittedToState | SubmittedToNational | State GS | - |
| `RejectByStateLeadershipAsync` | SubmittedToState | RejectedByState | State GS | - |
| `AcknowledgeByNationalAsync` | SubmittedToNational | Acknowledged | National leadership | - |

### Read-Only States
- **Acknowledged**: Final state (no further transitions)
- **StateReport SubmittedToNational**: Cannot edit once submitted (throws exception in `SaveStateReportAsync`)

---

## State Management Deep Dive

### State Management Strategy

The system uses a **hybrid state management approach**:

#### 1. **Database as Source of Truth**
- All persistent state stored in SQL Server
- EF Core entities mapped 1:1 to tables
- No in-memory state store
- Each query fetches fresh data from database

#### 2. **Component-Level Reactive State** (Blazor)
- Local component fields cache loaded data
- Manual `OnInitializedAsync` fetches from service layer
- Updates trigger re-render via `StateHasChanged()`
- No external state management library needed (too simple for Redux/Flux)

#### 3. **Service-Level Transient State**
- Services are scoped (per request in Blazor Server)
- No service-level caching beyond request scope
- `AmsaDirectoryLookupCache` is scoped exception (lookup cache)
- `AmsaTokenCache` is singleton (JWT token refresh optimization)

### State Transitions & Persistence

**All state transitions are synchronous database writes:**

```csharp
// Example: SubmitReportToPresidentAsync
public async Task<Report> SubmitReportToPresidentAsync(AuthContext actor, int reportId, string? notes = null, CancellationToken ct = default)
{
    var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == reportId, ct);

    // 1. Authorization check (no database change yet)
    if (!_access.CanInitiateReportSubmission(actor, report.UnitId, report.StateId))
        throw new UnauthorizedAccessException(...);

    // 2. State validation (check current status)
    if (report.Status != ReportStatus.Draft && report.Status != ReportStatus.RejectedByPresident)
        throw new InvalidOperationException(...);

    // 3. Dependency check (at least 1 dept submitted)
    if (!report.DepartmentReports.Any(d => d.IsSubmitted))
        throw new InvalidOperationException(...);

    // 4. State mutation
    report.Status = ReportStatus.SubmittedToPresident;
    report.SubmittedToPresidentAt = DateTime.UtcNow;
    report.SubmittedByMemberId = actor.MemberId;
    report.UpdatedAt = DateTime.UtcNow;

    // 5. Audit log entry
    _db.ReportActivityLogs.Add(new ReportActivityLog
    {
        ReportId = reportId,
        ActionByMemberId = actor.MemberId,
        Action = "ReportSubmittedToPresident",
        Notes = notes,
        ActionAt = DateTime.UtcNow
    });

    // 6. Atomic persist
    await _db.SaveChangesAsync(ct);
    return report;
}
```

**Key Properties of This Pattern:**
- **Atomicity**: Single `SaveChangesAsync` call = one transaction
- **Auditability**: Parallel activity log entries for all actions
- **Idempotency Checks**: All methods validate preconditions before mutation
- **Authorization First**: Access control checked before any database change
- **Fail-Fast**: Exceptions thrown early prevent partial updates

---

## Module Integration Map

### How Components Talk to Services

```
Dashboard.razor (Router)
├── NationalLeadershipDashboard.razor
│   └── Injects: CurrentUserReportService
│       └── Calls: GetNationalReportsWithStateContextAsync()
│           ├── Extracts current user via AMSAAuthStateProvider
│           └── Delegates to UnifiedReportService.GetNationalReportsWithStateContextAsync()
│               ├── Authorization check: IsNationalLeadership()
│               ├── Loads Reports + DepartmentReports (EF Include)
│               ├── Loads StateReports + StateReportPrograms (per state)
│               ├── Maps to ReportWithStateContext DTO
│               └── Returns composite view
├── StateLeadershipDashboard.razor
│   └── Similar pattern for state-level operations
├── UnitLeadershipDashboard.razor
│   └── Similar pattern for unit-level operations
└── DepartmentOfficerDashboard.razor
    └── Limited to department edit operations
```

### Service Dependency Injection

```
Program.cs
├── AddDbContext<AMSAReportingDbContext>()
├── Configure AmsaApiClientOptions
├── AddSingleton<AmsaTokenCache>()
├── AddHttpClient<IAmsaApiClient, AmsaApiClient>()
├── AddScoped<AmsaAuthService>()
├── AddScoped<AMSAAuthStateProvider>()
├── AddScoped<AuthenticationStateProvider>() [via AMSAAuthStateProvider]
├── AddScoped<ReportAccessService>()
├── AddScoped<UnifiedReportService>()
├── AddScoped<CurrentUserReportService>()
└── AddScoped<AmsaDirectoryLookupCache>()

SCOPES:
- Singleton: AmsaTokenCache, logging infrastructure
- Scoped (per request): All services, DbContext, auth providers
- Transient: AmsaApiClient (HttpClientFactory manages lifetime)
```

---

## Data Flow Patterns

### Pattern 1: Department Report Submission

```
User fills form in DepartmentOfficerDashboard
   │
   ▼
Component calls CurrentUserReportService.SaveDepartmentJsonAsync()
   │
   ▼
Service extracts current user via AMSAAuthStateProvider.GetCurrentUser()
   │
   ▼
Delegates to UnifiedReportService.SaveDepartmentDataAsync(reportId, dept, json, actor, markSubmitted, ct)
   │
   ├─ Authorization check: CanEditDepartment(actor, unitId, stateId, department)
   ├─ Load Report entity (fetch)
   ├─ Load/Create DepartmentReport entity (fetch or new)
   ├─ Update: ReportData = json, IsSubmitted = markSubmitted
   ├─ Add: ReportActivityLog entry
   └─ SaveChangesAsync() → Persists both entities
   │
   ▼
Service returns updated DepartmentReport entity
   │
   ▼
Component state updated locally, UI re-renders
```

### Pattern 2: State-Level Aggregation Trigger

```
State GS saves state report form via SaveMyStateReportAsync()
   │
   ├─ Load StateReport + Programs (EF Include)
   ├─ Update leadership commentary fields
   ├─ Call RecomputeStateAggregateAsync()
   │   │
   │   ├─ Query all unit Reports for this state+cycle
   │   ├─ Count submitted-to-state-or-higher reports
   │   ├─ Query all DepartmentReports (submitted only)
   │   ├─ Group by Department type
   │   ├─ Remove existing auto-aggregated StateReportProgram rows
   │   ├─ Create new StateReportProgram rows (IsAutoAggregated=true)
   │   │   └─ TotalAttendance = sum(DepartmentReport.AttendanceCount)
   │   │   └─ TotalBeneficiaries = sum(DepartmentReport.BeneficiaryCount)
   │   └─ Mark: IsAggregated=true, LastAggregatedAt=now
   │
   ├─ Update status if markSubmitted
   └─ SaveChangesAsync() → Atomic persist
```

### Pattern 3: National Dashboard Load

```
National leadership navigates to NationalLeadershipDashboard
   │
   ▼
Component OnInitializedAsync() calls:
  CurrentUserReportService.GetNationalReportsWithStateContextAsync(cycleId)
   │
   ├─ Extract current user from AMSAAuthStateProvider
   ├─ Check: IsNationalLeadership(actor)
   ├─ Load all Reports for cycle (with Cycle, DepartmentReports, ActivityLogs)
   ├─ For each Report:
   │   ├─ Load StateReport + Programs for same state+cycle
   │   ├─ Map Report → ReportDepartmentView (DTO)
   │   ├─ Map StateReport → StateReportContext (DTO)
   │   ├─ Map ActivityLogs → ReportActivityView (DTO)
   │   └─ Create ReportWithStateContext (composite DTO)
   │
   ▼
Return List<ReportWithStateContext>
   │
   ▼
Component caches list in local state
Component calls WarmReportNamesAsync():
   ├─ Extract unique UnitIds + StateIds
   ├─ Call DirectoryLookupCache.GetUnitNameAsync(unitId) for each
   ├─ Call DirectoryLookupCache.GetStateNameAsync(stateId) for each
   └─ Cache populates from AMSA API (on cache miss)
   │
   ▼
Component renders table from ReportWithStateContext list
```

### Pattern 4: Access Control Authorization Check

```
Component tries to call service method requiring auth
   │
   ▼
Service method receives actor: AuthContext
   │
   ├─ Extract parsed roles from actor.ParsedRoles
   ├─ Check against hierarchical role matrix:
   │   ├─ National leadership? (HasLeadershipAtLevel(actor, National))
   │   ├─ State leadership + matching state? (stateId == actor.StateId && IsStateLeadership)
   │   ├─ Unit leadership + matching unit? (unitId == actor.UnitId && IsUnitLeadership)
   │   ├─ Department officer in this unit?
   │   └─ National department head?
   │
   ▼
If all checks fail: throw UnauthorizedAccessException
Else: proceed with business logic
```

---

## Authorization & Access Control

### Role-Based Access Control (RBAC) Hierarchy

#### Role Structure

```csharp
// Stored as "DepartmentName:LevelType" strings
// Example: "Taleem:Unit", "General:State", "President:National"

public List<string> Roles { get; set; } // Raw roles
public List<(string DepartmentName, LevelType LevelType)> ParsedRoles { get; set; } // Parsed
```

#### Department Types (9 + Leadership)
1. **Taleem** (Islamic Education)
2. **Tabligh** (Tabligh/Outreach)
3. **Welfare** (Social Services)
4. **Sport**
5. **Finance**
6. **Health**
7. **SecondarySchool**
8. **Tajneed** (Genealogy/Records)
9. **General** (General/Internal Affairs)
10. **President** (Leadership)
11. **VicePresident** (Leadership, National only)

#### Access Control Rules

**Department-Level Edit Access** (`CanEditDepartment`):
```
Permission granted if ANY of:
1. National leadership (any level)
2. State leadership + same StateId + StateLevel role
3. Unit leadership + same UnitId + UnitLevel role
4. State department officer + same StateId + SameDepartment:State
5. Unit department officer + same UnitId + SameDepartment:Unit
6. National department officer + SameDepartment:National
```

**Report Submission Initiation** (`CanInitiateReportSubmission`):
```
Permission granted if ANY of:
1. National leadership
2. State leadership + same StateId
3. Unit leadership + same UnitId
4. Department officer in same unit + same state
```

**Report Review/Approval** (`CanReviewAtUnitLevel` / `CanReviewAtStateLevel`):
```
Unit level: National leadership OR (same UnitId AND IsUnitLeadership)
State level: National leadership OR (same StateId AND IsStateLeadership)
```

#### Leadership Department Detection
```csharp
private static bool IsLeadershipDepartment(string departmentName, LevelType levelType)
{
    // Normalized: "president", "general", "vicepresident", "vpadmin", etc.

    // VP roles only valid at National level
    if (IsVpDepartment(normalized) && levelType != LevelType.National)
        return false;

    return normalized is "president" or "general" or "vicepresident" or "vpadmin" or ...;
}
```

#### Sudo Access
- Any member with ANY leadership role gets `HasSudoAccess = true`
- Sudo access grants national-level permissions

---

## Composite Views & DTO Architecture

### DTO Types (from NationalReportDTOs.cs)

#### `ReportWithStateContext` (Composite)
**Purpose**: National leadership sees unit report + state context in one view

```csharp
public record ReportWithStateContext(
    int ReportId,
    int UnitId,
    int StateId,
    string CycleLabel,
    ReportStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? SubmittedToPresidentAt,
    DateTime? ApprovedByPresidentAt,
    string? PresidentialNotes,
    DateTime? ApprovedByStateAt,
    string? StateNotes,
    DateTime? AcknowledgedByNationalAt,
    string? NationalNotes,

    // NEW: State leadership context from StateReport
    StateReportContext? StateContext,

    // Original department data
    List<ReportDepartmentView> Departments,

    // Activity timeline
    List<ReportActivityView> ActivityLogs);
```

**Data Sources**:
- Report entity → ReportId, UnitId, StateId, Status, timestamps, notes
- StateReport entity → StateContext (if exists)
- DepartmentReports → Departments list
- ReportActivityLogs → ActivityLogs list

#### `StateReportContext` (State Leadership Info)
**Purpose**: Nested in ReportWithStateContext; shows state GS's aggregated view

```csharp
public record StateReportContext(
    int StateReportId,
    string CycleLabel,
    int UnitsAttendedTo,           // Count of units that submitted
    int TotalUnitReportsCount,     // Total units in state
    int? UnitPerformanceRating,    // 0-100 rating
    decimal UnitPresidentAttendanceRate, // Computed %

    // State GS commentary
    string? UnitImprovementPlan,
    string? ChallengesFaced,
    string? NationalSupportNeeded,
    string? OtherNotes,

    // Approval chain
    string? PresidentialNote,
    DateTime? ApprovedByPresidentAt,
    string? NationalNote,
    DateTime? AcknowledgedByNationalAt,

    // Aggregation metadata
    bool IsAggregated,
    DateTime? LastAggregatedAt,

    // Aggregated programs
    List<StateReportProgramView> Programs);
```

#### `StateReportProgramView`
**Purpose**: Program-level metrics in state aggregation

```csharp
public record StateReportProgramView(
    int ProgramId,
    string ProgramName,            // e.g., "Taleem", "Tabligh"
    string? Objectives,
    string? Outcomes,
    int? TotalAttendance,          // Sum across units
    int? TotalBeneficiaries,       // Sum across units
    bool IsAutoAggregated);        // Flag for preservation during re-aggregation
```

#### `ReportDepartmentView` & `ReportActivityView`
**Purpose**: Display-ready projections

```csharp
// In NationalReportDTOs.cs (inferred from code)
public record ReportDepartmentView(
    DepartmentType Department,
    string DepartmentName,
    bool IsSubmitted,
    DateTime? SubmittedAt,
    string ReportData);            // JSON payload

public record ReportActivityView(
    DateTime ActionAt,
    string Action,
    string? Notes);
```

### DTO Composition in GetNationalReportsWithStateContextAsync

```csharp
// For each Report in cycle:
var reportWithContext = new ReportWithStateContext(
    ReportId: report.Id,
    // ... simple properties ...

    // COMPOSITE: Get state report if it exists
    StateContext: stateReport is null ? null : new StateReportContext(
        StateReportId: stateReport.Id,
        // ... state metrics ...
        Programs: [.. stateReport.Programs
            .Select(p => new StateReportProgramView(
                p.ProgramId,
                p.ProgramName,
                p.Objectives,
                p.Outcomes,
                p.TotalAttendance,
                p.TotalBeneficiaries,
                p.IsAutoAggregated))],
    ),

    // Department data from DepartmentReports
    Departments: [.. report.DepartmentReports
        .OrderBy(d => d.Department)
        .Select(d => new ReportDepartmentView(
            d.Department, 
            d.Department.ToString(), 
            d.IsSubmitted, 
            d.SubmittedAt, 
            d.ReportData))],

    // Activity logs
    ActivityLogs: [.. report.ActivityLogs
        .OrderByDescending(log => log.ActionAt)
        .Select(log => new ReportActivityView(...))]
);
```

### Benefits of Composite DTOs

1. **Single Network Roundtrip**: One method call loads report + state context
2. **Null Safety**: StateContext can be null if state hasn't created report yet
3. **Presentation Optimized**: DTOs map to UI needs exactly
4. **No N+1 Problem**: StateReport loaded in batch for all units
5. **Immutable Records**: Compile-time safety with C# 14 records

---

## Key Design Decisions

### 1. **Unified Report Service Over Segregated Services**
- ✅ **Chosen**: `UnifiedReportService` (monolithic 700+ LOC)
- ❌ **Alternative**: DepartmentReportService, StateReportService, ReportLifecycleService (mentioned in docstring as "consolidated")
- **Reasoning**: 
  - Single service for all report ops maintains transaction boundaries
  - Cross-cutting authorization logic in one place
  - Easier to test workflows involving multiple services
  - Reduces risk of partial state updates across services

### 2. **CurrentUserReportService Wrapper Pattern**
- ✅ **Chosen**: Wrapper service that extracts current user automatically
- ❌ **Alternative**: Pass AuthContext explicitly in every component call
- **Reasoning**: 
  - Reduces boilerplate in 4+ dashboard components
  - Centralized user extraction logic
  - Aligns with Blazor cascading parameters pattern

### 3. **Stateless Services + EF Core as State Repository**
- ✅ **Chosen**: No service-level caching; EF queries database each time
- ❌ **Alternative**: Redis/memory cache of open reports
- **Reasoning**: 
  - Reports are short-lived (monthly cycle)
  - Multiple users can edit simultaneously (cache invalidation pain)
  - EF Core DbContext is light-weight for scoped lifetime
  - Audit trail (ActivityLogs) relies on single DB source of truth

### 4. **Composite DTO for National Dashboard**
- ✅ **Chosen**: `ReportWithStateContext` combines Report + StateReport data
- ❌ **Alternative**: Two separate queries (Report, StateReport) in component
- **Reasoning**: 
  - National leadership needs both in single view
  - Eliminates N+1 queries (StateReport loaded in loop)
  - Clean separation: data mapping in service, not component

### 5. **Hybrid Model for DepartmentReport**
- ✅ **Chosen**: JSON in `ReportData` + extracted denormalized fields
- ❌ **Alternative**: Full relational schema per department OR only JSON
- **Reasoning**: 
  - JSON flexibility: question schema can change per department/cycle
  - Denormalized fields: fast aggregation queries (e.g., sum(AttendanceCount))
  - Avoids EAV anti-pattern while supporting schema evolution

### 6. **Auto-Aggregation with Preservation Logic**
- ✅ **Chosen**: Remove only `IsAutoAggregated=true` rows on re-aggregation
- ❌ **Alternative**: Clear all programs, re-aggregate everything
- **Reasoning**: 
  - State GS can manually add programs for state-only initiatives
  - Re-aggregation preserves manual entries
  - Audit trail visible in LastAggregatedAt timestamp

### 7. **Role-Based Hierarchical RBAC**
- ✅ **Chosen**: 3-level hierarchy (Unit, State, National) + department-specific permissions
- ❌ **Alternative**: Flat permission set (e.g., EDIT_REPORT, APPROVE_REPORT, etc.)
- **Reasoning**: 
  - AMSA organization is inherently hierarchical
  - Department-specific access controls necessary (Taleem officer edits Taleem only)
  - VP roles only valid at National level (org constraint)
  - Reduces permission explosion

### 8. **Monthly Cycle Determinism**
- ✅ **Chosen**: One cycle per calendar month; deadline = last day of month
- ❌ **Alternative**: Admin-defined cycles with flexible deadlines
- **Reasoning**: 
  - AMSA operates on monthly reporting cadence
  - Predictable for members (no ambiguity)
  - `EnsureActiveCycleAsync` is idempotent (safe to call any time)
  - Last day of month is natural reporting deadline

### 9. **Cascade Delete from Report → DepartmentReports**
- ✅ **Chosen**: OnDelete(DeleteBehavior.Cascade)
- **Reasoning**: 
  - DepartmentReport has no standalone meaning (always associated with Report)
  - Deleting a report should clean up all departments atomically
  - Alternative (SetNull) doesn't make sense (DepartmentReport requires ReportId)

### 10. **ReportingCycle Restrict Delete**
- ✅ **Chosen**: OnDelete(DeleteBehavior.Restrict) for Cycle → Report and Cycle → StateReport
- **Reasoning**: 
  - Cycles are historical records
  - Should not delete cycles with active/submitted reports
  - Prevents accidental data loss
  - Can archive cycles instead

---

## Summary: How Everything Integrates

### User Journey: Submit Unit Report → National Approval

```
1. UNIT MEMBER EDITS DEPARTMENT FORM
   DepartmentOfficerDashboard.razor
   └─ CurrentUserReportService.SaveDepartmentJsonAsync(reportId, Taleem, json, true)
      └─ UnifiedReportService.SaveDepartmentDataAsync()
         ├─ ReportAccessService.CanEditDepartment() → ✓
         ├─ Load Report, Create/Update DepartmentReport
         ├─ Add ReportActivityLog("DepartmentReportSubmitted")
         └─ _db.SaveChangesAsync() [ATOMIC]

2. UNIT PRESIDENT APPROVES REPORT
   UnitLeadershipDashboard.razor
   └─ CurrentUserReportService.ApproveAtUnitAsync(reportId, "Looks good")
      └─ UnifiedReportService.ApproveByUnitLeadershipAsync()
         ├─ ReportAccessService.CanReviewAtUnitLevel() → ✓
         ├─ Validate: Report.Status = SubmittedToPresident
         ├─ Update: Report.Status = SubmittedToState
         ├─ Add ReportActivityLog("ApprovedByUnitLeadership")
         └─ _db.SaveChangesAsync() [ATOMIC]

3. STATE GS REVIEWS UNIT DATA & AGGREGATES STATE REPORT
   StateLeadershipDashboard.razor
   └─ CurrentUserReportService.SaveMyStateReportAsync(stateReportId, form, true)
      └─ UnifiedReportService.SaveStateReportAsync()
         ├─ ReportAccessService.CanReviewAtStateLevel() → ✓
         ├─ Update: StateReport leadership fields
         ├─ Call: RecomputeStateAggregateAsync()
         │   ├─ Query all units in state (this cycle)
         │   ├─ Count submitted-or-higher reports
         │   ├─ Aggregate DepartmentReports by type
         │   ├─ Remove auto-aggregated StateReportPrograms
         │   ├─ Create new StateReportPrograms with sums
         │   └─ Mark: IsAggregated=true, LastAggregatedAt=now
         ├─ Update: StateReport.Status = SubmittedToNational
         ├─ Add ReportActivityLog("state-level action")
         └─ _db.SaveChangesAsync() [ATOMIC]

4. NATIONAL LEADERSHIP REVIEWS ALL REPORTS WITH STATE CONTEXT
   NationalLeadershipDashboard.razor
   └─ CurrentUserReportService.GetNationalReportsWithStateContextAsync(cycleId)
      └─ UnifiedReportService.GetNationalReportsWithStateContextAsync()
         ├─ ReportAccessService.IsNationalLeadership() → ✓
         ├─ Query: All Reports for cycle (with includes)
         ├─ For each Report:
         │   ├─ Query: StateReport + Programs for state+cycle
         │   ├─ Map: Report → ReportWithStateContext
         │   ├─ Map: StateReport → StateReportContext (nested)
         │   └─ Compose DTOs
         ├─ Return: List<ReportWithStateContext>
         │
         └─ Component renders table:
            ├─ One row per unit report
            ├─ State context shown as column (Units attended / Performance rating)
            ├─ Department breakdown visible
            ├─ Activity timeline shown on expand
            └─ Acknowledge button for each

5. NATIONAL LEADERSHIP ACKNOWLEDGES REPORT
   NationalLeadershipDashboard.razor (Acknowledge button click)
   └─ CurrentUserReportService.AcknowledgeAtNationalAsync(reportId, notes)
      └─ UnifiedReportService.AcknowledgeByNationalAsync()
         ├─ ReportAccessService.IsNationalLeadership() → ✓
         ├─ Validate: Report.Status = SubmittedToNational
         ├─ Update: Report.Status = Acknowledged
         ├─ Add ReportActivityLog("AcknowledgedByNational")
         └─ _db.SaveChangesAsync() [ATOMIC]

6. REPORT IS COMPLETE
   └─ Report.Status = Acknowledged (read-only state)
   └─ StateReport read-only (status check prevents further edits)
   └─ Activity log preserved entire workflow timeline
```

### Data Flow Architecture

```
┌─────────────────────────────────────────────────────────────┐
│ PRESENTATION LAYER (Blazor Components)                      │
│ - Dashboard.razor (router)                                  │
│ - NationalLeadershipDashboard.razor                         │
│ - StateLeadershipDashboard.razor                            │
│ - UnitLeadershipDashboard.razor                             │
│ - DepartmentOfficerDashboard.razor                          │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       │ Injection: CurrentUserReportService
                       │
┌──────────────────────▼──────────────────────────────────────┐
│ SERVICE LAYER (Business Logic + Authorization)              │
│ - CurrentUserReportService (user extraction wrapper)        │
│ - UnifiedReportService (core business logic)                │
│   ├─ Department Report Operations                           │
│   ├─ State Report Operations                                │
│   ├─ Report Lifecycle & Submission                          │
│   └─ Reporting Cycle Management                             │
│ - ReportAccessService (RBAC checks)                         │
│ - AmsaAuthService (auth + role parsing)                     │
│ - AmsaDirectoryLookupCache (unit/state name caching)        │
│ - AmsaApiClient (external API integration)                  │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       │ DbContext queries/commands
                       │
┌──────────────────────▼──────────────────────────────────────┐
│ DATA ACCESS LAYER (Entity Framework Core)                   │
│ - AMSAReportingDbContext                                    │
│   ├─ ReportingCycle (monthly periods)                       │
│   ├─ Report (unit-level aggregation)                        │
│   ├─ DepartmentReport (9 department entries)                │
│   ├─ StateReport (state aggregation + commentary)           │
│   ├─ StateReportProgram (auto-aggregated programs)          │
│   └─ ReportActivityLog (audit trail)                        │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       │ SQL queries
                       │
┌──────────────────────▼──────────────────────────────────────┐
│ DATABASE LAYER (SQL Server)                                 │
│ - ReportingCycles table (index: CycleMonth)                 │
│ - Reports table (indices: UnitId+CycleId, Status, StateId) │
│ - DepartmentReports table (index: ReportId+Department)      │
│ - StateReports table (index: StateId+CycleId)               │
│ - StateReportPrograms table (index: StateReportId)          │
│ - ReportActivityLogs table (index: ReportId)                │
└─────────────────────────────────────────────────────────────┘

EXTERNAL INTEGRATIONS:
├─ AMSA REST API (member lookup, unit/state directory)
│  └─ Via: AmsaApiClient (typed HttpClient)
│  └─ Caching: AmsaTokenCache (singleton), AmsaDirectoryLookupCache (scoped)
│
└─ Azure AD / Authentication Provider
   └─ Via: AmsaAuthService (JWT token handling)
   └─ State: AMSAAuthStateProvider (cascading auth state)
```

### Key Takeaways

1. **Monolithic Service, Layered Architecture**: UnifiedReportService handles all business logic; ReportAccessService handles auth
2. **EF Core as Source of Truth**: No redundant caching; database queries per request
3. **Composite DTOs for Complex Views**: ReportWithStateContext solves N+1 and presentation needs
4. **State Transitions via Methods**: Clear state machine; each method validates preconditions
5. **Atomic Transactions**: Single SaveChangesAsync per workflow action
6. **Audit Trail First Class**: ReportActivityLog captures every state change
7. **Hierarchical RBAC**: 3 levels + department-specific; clear permission model
8. **Role-Based Dashboards**: 4 dashboard types; component routing based on parsed roles
9. **Async/Await Throughout**: All I/O is async; proper CancellationToken support
10. **Strongly Typed Over Strings**: Enums for status, departments, levels; parsed roles avoid magic strings

---

## Diagram: Complete System Architecture

```
┌────────────────────────────────────────────────────────────────────────┐
│                      BLAZOR SERVER/WASM FRONTEND                       │
├────────────────────────────────────────────────────────────────────────┤
│                                                                        │
│  Dashboard Router → Role-Based Dashboard Selector                     │
│  ├─ @role NationalLeadership  → NationalLeadershipDashboard          │
│  ├─ @role StateLeadership     → StateLeadershipDashboard             │
│  ├─ @role UnitLeadership      → UnitLeadershipDashboard              │
│  └─ @role DepartmentOfficer   → DepartmentOfficerDashboard           │
│                                                                        │
│  Each Dashboard: Component → CurrentUserReportService → UI rendering  │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
                                   │
                                   │ HTTP/Blazor Requests
                                   │
┌────────────────────────────────────────────────────────────────────────┐
│                         ASP.NET Core Backend                           │
├────────────────────────────────────────────────────────────────────────┤
│                                                                        │
│  ┌─ Program.cs                                                        │
│  │  ├─ DbContext registration (SQL Server)                           │
│  │  ├─ Service registrations (scoped, transient)                     │
│  │  ├─ HttpClient factories (AMSA API)                               │
│  │  └─ Authentication/Authorization setup                           │
│  │                                                                    │
│  ├─ CurrentUserReportService (convenience wrapper)                   │
│  │  └─ Extracts user context → delegates to UnifiedReportService   │
│  │                                                                    │
│  ├─ UnifiedReportService (core business logic)                       │
│  │  ├─ Department Report Ops (save, submit)                         │
│  │  ├─ State Report Ops (aggregation, lifecycle)                    │
│  │  ├─ Report Lifecycle Ops (approval workflow)                     │
│  │  ├─ Reporting Cycle Ops (monthly period management)              │
│  │  └─ Aggregation Engine (sum DepartmentReports → Programs)        │
│  │                                                                    │
│  ├─ ReportAccessService (authorization)                              │
│  │  ├─ Role parsing (DepartmentName:LevelType)                      │
│  │  ├─ Hierarchical permission checks                               │
│  │  └─ Department-specific access control                           │
│  │                                                                    │
│  ├─ AmsaAuthService                                                  │
│  │  ├─ Member lookup (by MKAN)                                      │
│  │  ├─ JWT generation & validation                                  │
│  │  └─ Role parsing                                                 │
│  │                                                                    │
│  ├─ AMSAAuthStateProvider (cascading auth state)                    │
│  │  ├─ Current user context retrieval                               │
│  │  └─ Token refresh management                                     │
│  │                                                                    │
│  ├─ AmsaDirectoryLookupCache (lookup optimization)                  │
│  │  ├─ In-memory cache (scoped)                                     │
│  │  └─ AMSA API fallback on cache miss                              │
│  │                                                                    │
│  └─ AmsaApiClient (external integration)                             │
│     ├─ HTTP requests to AMSA backend                                │
│     ├─ Token management (AmsaTokenCache singleton)                  │
│     └─ Endpoints: /members, /states, /units, /auth/token           │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
                                   │
                                   │ SQL Queries via EF Core
                                   │
┌────────────────────────────────────────────────────────────────────────┐
│                      SQL Server Database                               │
├────────────────────────────────────────────────────────────────────────┤
│                                                                        │
│  ┌─ ReportingCycle                                                   │
│  │  └─ Unique constraint: (CycleMonth) - monthly periods             │
│  │                                                                    │
│  ├─ Report (1 per unit per cycle)                                    │
│  │  ├─ Unique constraint: (UnitId, CycleId)                         │
│  │  ├─ Status: [Draft → SubmittedToPresident → SubmittedToState    │
│  │  │          → SubmittedToNational → Acknowledged]               │
│  │  └─ Tracking: SubmittedAt, ApprovedAt (3 levels)                 │
│  │                                                                    │
│  ├─ DepartmentReport (9 per Report)                                  │
│  │  ├─ Unique constraint: (ReportId, Department)                    │
│  │  ├─ Hybrid model: ReportData (JSON) + denormalized fields       │
│  │  └─ Fields: AttendanceCount, SessionsOrganized, DuesCollected   │
│  │                                                                    │
│  ├─ StateReport (1 per state per cycle)                              │
│  │  ├─ Unique constraint: (StateId, CycleId)                        │
│  │  ├─ Metrics: UnitsAttendedTo, TotalUnitReportsCount             │
│  │  ├─ Commentary: UnitImprovementPlan, ChallengesFaced            │
│  │  └─ Aggregation flag: IsAggregated, LastAggregatedAt            │
│  │                                                                    │
│  ├─ StateReportProgram (auto-aggregated programs)                    │
│  │  ├─ ProgramName: grouped by DepartmentType                       │
│  │  ├─ Metrics: TotalAttendance (sum), TotalBeneficiaries (sum)    │
│  │  └─ Flag: IsAutoAggregated (preserved on re-aggregation)        │
│  │                                                                    │
│  └─ ReportActivityLog (audit trail)                                  │
│     ├─ Action: submitted, approved, rejected, acknowledged           │
│     ├─ ActionByMemberId: who performed action                        │
│     └─ Notes: optional context (e.g., rejection reason)             │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

**END OF ANALYSIS**

This architecture represents a well-structured, hierarchical reporting system with clear separation of concerns, strong authorization controls, and atomic state management. The system prioritizes data consistency, auditability, and role-based access patterns aligned with the AMSA organizational structure.
