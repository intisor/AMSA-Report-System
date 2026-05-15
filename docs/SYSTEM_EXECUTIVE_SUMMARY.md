# AMSA Reporting System - Executive Summary

## What This System Does

The **AMSA Reporting System** is a compliance and data collection platform for the AMSA organization's monthly reporting requirements. It enables:

- **Monthly report submissions** from ~100+ units across all states
- **9 department-specific reports** per unit (Taleem, Tabligh, Welfare, Sport, Finance, Health, SecondarySchool, Tajneed, General)
- **Multi-level approval workflow** (Unit President → State GS → National Leadership)
- **State-level aggregation** combining all unit data with state-specific commentary
- **National dashboard** giving leadership full visibility into organizational performance

---

## System Architecture (5 Layers)

```
┌─────────────────────────────────────┐
│   1. PRESENTATION (Blazor)          │ ← User dashboards, forms, tables
├─────────────────────────────────────┤
│   2. SERVICE LAYER                  │ ← Business logic, authorization
├─────────────────────────────────────┤
│   3. DATA ACCESS (EF Core)          │ ← Database queries
├─────────────────────────────────────┤
│   4. DATABASE (SQL Server)          │ ← Persistent state
├─────────────────────────────────────┤
│   5. EXTERNAL (AMSA API)            │ ← Member/unit/state lookups
└─────────────────────────────────────┘
```

---

## Core Entities & Relationships

### 6 Main Database Tables

| Entity | Purpose | Unique Key | Count |
|--------|---------|-----------|-------|
| **ReportingCycle** | Monthly period (Jan, Feb, etc.) | CycleMonth | ~1-2 active |
| **Report** | Unit's aggregated report for cycle | UnitId + CycleId | ~100-200 per cycle |
| **DepartmentReport** | One department's submission | ReportId + Department | 900 (9 depts × 100 units) |
| **StateReport** | State-level aggregation + commentary | StateId + CycleId | ~50 per cycle |
| **StateReportProgram** | Auto-aggregated programs by department | StateReportId | ~400 (8 depts × 50 states) |
| **ReportActivityLog** | Audit trail of all actions | ReportId | Grows indefinitely |

### Data Model Relationships

```
ReportingCycle (1) ─────< (many) Report
                   └────< (many) StateReport

Report (1) ─────< (many) DepartmentReport
      (1) ─────< (many) ReportActivityLog

StateReport (1) ─────< (many) StateReportProgram
```

---

## Report Workflow (State Machine)

```
DRAFT
  ├─ Submit to President
  │   ↓
  │ SUBMITTED_TO_PRESIDENT
  │   ├─ Unit President Approves
  │   │   ↓
  │   │ SUBMITTED_TO_STATE
  │   │   ├─ State GS Approves
  │   │   │   ↓
  │   │   │ SUBMITTED_TO_NATIONAL
  │   │   │   ├─ National Acknowledges
  │   │   │   │   ↓
  │   │   │   │ ACKNOWLEDGED (FINAL)
  │   │   │   │
  │   │   │   └─ Reject → back to DRAFT
  │   │   │
  │   │   └─ Reject → REJECTED_BY_STATE → back to DRAFT
  │   │
  │   └─ Reject → REJECTED_BY_PRESIDENT → back to DRAFT
  │
  └─ OR create in DRAFT, never submit
```

**Constraints per Transition:**
- Submit to President: ≥1 department submitted
- Approve by Unit: ≥1 department submitted
- Approve by State: Must be in SubmittedToState status
- Acknowledge by National: Must be in SubmittedToNational status

---

## State Management Strategy

### The Rule: "Database is the Source of Truth"

**No caching. No in-memory state store. No Redis. Just:**

1. **Component loads data** via service call
2. **Service queries database** (EF Core)
3. **User performs action** (submit, approve, etc.)
4. **Service validates + mutates** in single atomic transaction
5. **Database persists** (SaveChangesAsync)
6. **Component re-renders** with fresh data

### Service Lifetimes

| Service | Lifetime | Why |
|---------|----------|-----|
| `UnifiedReportService` | Scoped | One instance per request; ensures clean EF context |
| `CurrentUserReportService` | Scoped | Wraps UnifiedReportService + extracts user |
| `ReportAccessService` | Can be instantiated per-call | Stateless authorization checks |
| `AmsaTokenCache` | Singleton | Shares JWT tokens across requests (optimization) |
| `AmsaDirectoryLookupCache` | Scoped | Lookup cache per request (prevents repeated API calls) |

### Transaction Boundaries

Every workflow action (submit, approve, reject, acknowledge) is a single atomic transaction:

```csharp
// 1. Load entity
var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == reportId);

// 2. Check authorization (no DB change yet)
if (!_access.CanInitiateReportSubmission(actor, report.UnitId, report.StateId))
    throw new UnauthorizedAccessException(...);

// 3. Validate preconditions
if (!report.DepartmentReports.Any(d => d.IsSubmitted))
    throw new InvalidOperationException("At least one department must be submitted");

// 4. Mutate
report.Status = ReportStatus.SubmittedToPresident;
report.SubmittedToPresidentAt = DateTime.UtcNow;

// 5. Add audit log
_db.ReportActivityLogs.Add(new ReportActivityLog { ... });

// 6. ATOMIC COMMIT
await _db.SaveChangesAsync(); // ← Single transaction
```

---

## Module Integration Map

### How Components Talk to Services

```
Component (Dashboard.razor)
  │
  ├─ Inject: CurrentUserReportService
  │   │
  │   └─ Extracts: AMSAAuthStateProvider.GetCurrentUser()
  │
  └─ Calls: CurrentUserReportService.SaveDepartmentJsonAsync(...)
      │
      └─ Delegates to: UnifiedReportService.SaveDepartmentDataAsync(...)
          │
          ├─ Auth Check: ReportAccessService.CanEditDepartment(actor, ...)
          ├─ Load: Report + DepartmentReport entities
          ├─ Validate: JSON, preconditions
          ├─ Mutate: Update ReportData, IsSubmitted
          ├─ Audit: Add ReportActivityLog
          └─ Persist: _db.SaveChangesAsync() [ATOMIC]
```

### Service Dependency Graph

```
Program.cs Registration:
  ├─ AddDbContext<AMSAReportingDbContext>()
  ├─ AddSingleton<AmsaTokenCache>()
  ├─ AddHttpClient<IAmsaApiClient, AmsaApiClient>()
  ├─ AddScoped<AmsaAuthService>()
  ├─ AddScoped<AMSAAuthStateProvider>()
  ├─ AddScoped<AuthenticationStateProvider>()
  ├─ AddScoped<ReportAccessService>()
  ├─ AddScoped<UnifiedReportService>()
  ├─ AddScoped<CurrentUserReportService>()
  └─ AddScoped<AmsaDirectoryLookupCache>()
```

---

## Authorization Model (RBAC)

### 3-Level Hierarchy

```
┌─────────────────────────────────┐
│   NATIONAL LEADERSHIP           │  (LevelType.National)
│   ├─ President                  │
│   ├─ General Secretary          │
│   └─ VPs (Admin, North, etc.)   │
├─────────────────────────────────┤
│   STATE LEADERSHIP              │  (LevelType.State)
│   ├─ State GS                   │
│   └─ State Department Heads     │
├─────────────────────────────────┤
│   UNIT LEADERSHIP               │  (LevelType.Unit)
│   ├─ Unit President             │
│   └─ Department Officers        │
└─────────────────────────────────┘
```

### Department-Specific Access

Users have roles like:
- `"Taleem:Unit"` → Can edit Taleem reports at unit level
- `"General:State"` → Can edit General reports at state level
- `"Finance:National"` → Can edit Finance reports across all states

### Permission Checks

```csharp
// Can edit this department?
CanEditDepartment(actor, unitId, stateId, Department.Taleem)
  ├─ National leadership? → YES
  ├─ State leader in this state? → YES
  ├─ Unit leader in this unit? → YES
  ├─ Taleem officer in this unit? → YES
  ├─ Taleem officer in this state? → YES
  ├─ National Taleem officer? → YES
  └─ Else → NO
```

---

## Data Flow: Department Report Submission

```
1. Unit member fills Taleem form in DepartmentOfficerDashboard
   └─ Form data: attendance, sessions organized, etc.

2. Clicks "Save & Submit"
   └─ Component calls: CurrentUserReportService.SaveDepartmentJsonAsync(
        reportId=42, 
        department=DepartmentType.Taleem, 
        reportDataJson="{...}", 
        markSubmitted=true)

3. Service extracts current user
   └─ actor = AMSAAuthStateProvider.GetCurrentUser()

4. Service delegates to UnifiedReportService
   └─ UnifiedReportService.SaveDepartmentDataAsync(reportId, Taleem, json, actor, true)

5. Authorization check
   ├─ ReportAccessService.CanEditDepartment(actor, unitId, stateId, Taleem)
   └─ Throws if unauthorized

6. Database operations (atomic transaction):
   ├─ Load Report entity
   ├─ Load/Create DepartmentReport entity
   ├─ Update: 
   │   DepartmentReport.ReportData = json
   │   DepartmentReport.IsSubmitted = true
   │   DepartmentReport.SubmittedAt = DateTime.UtcNow
   │   DepartmentReport.SubmittedByMemberId = actor.MemberId
   ├─ Extract analytics from JSON:
   │   DepartmentReport.AttendanceCount = json["attendance"]
   │   etc.
   ├─ Create audit log:
   │   ReportActivityLog(Action="DepartmentReportSubmitted", ...)
   └─ SaveChangesAsync() ← ATOMIC

7. Component receives updated DepartmentReport
   └─ Local state updated, UI re-renders

8. Eventually, unit president reviews all 9 departments
   └─ If all submitted (or acceptable subset), approves report
   └─ Report transitions: Draft → SubmittedToState
```

---

## Data Flow: National Dashboard Load

```
1. National leadership navigates to NationalLeadershipDashboard.razor

2. Component OnInitializedAsync() calls:
   └─ CurrentUserReportService.GetNationalReportsWithStateContextAsync(cycleId)

3. Service extracts current user
   └─ actor = AMSAAuthStateProvider.GetCurrentUser()

4. Service checks authorization
   └─ ReportAccessService.IsNationalLeadership(actor)
   └─ Throws if not national leadership

5. Database queries (non-transactional, read-only):
   ├─ Load all Reports for cycle (with Include: Cycle, DepartmentReports, ActivityLogs)
   ├─ For EACH Report:
   │   ├─ Load StateReport + Programs for same state+cycle
   │   ├─ Map Report → ReportDepartmentView[] (DTO)
   │   ├─ Map StateReport → StateReportContext (DTO)
   │   ├─ Map ActivityLogs → ReportActivityView[] (DTO)
   │   └─ Create ReportWithStateContext (COMPOSITE DTO)
   └─ Return List<ReportWithStateContext>

6. Component warms directory cache:
   ├─ Extract unique UnitIds from reports
   ├─ Call DirectoryLookupCache.GetUnitNameAsync(unitId)
   │   └─ Looks up in scoped cache
   │   └─ On miss: calls AMSA API
   ├─ Extract unique StateIds
   ├─ Call DirectoryLookupCache.GetStateNameAsync(stateId)
   │   └─ Same pattern
   └─ Cache populated without repeated API calls

7. Component renders table:
   ├─ Rows: One per unit report
   ├─ Columns:
   │   ├─ State name
   │   ├─ Unit name
   │   ├─ Report status
   │   ├─ Date submitted to state
   │   ├─ State context (units attended / total / rating)
   │   ├─ National notes input
   │   └─ Acknowledge button
   ├─ Expandable: Shows department breakdown + activity timeline
   └─ Interactive: Acknowledge button per report
```

---

## Composite DTO: ReportWithStateContext

### Why It Exists

National leadership needs:
1. **Unit report data** (status, departments, timeline)
2. **State context** (state GS's commentary, unit performance rating, aggregated programs)

**Without composite DTO:**
- Component makes 2 queries per report (N+1 problem)
- Data mapping scattered in component code
- Null handling everywhere

**With composite DTO:**
- 1 method call per batch
- Clean structure (nested records)
- State context nullable (state may not have created StateReport yet)
- Mapping logic in service, not component

### Structure

```csharp
public record ReportWithStateContext(
    // Unit-level report data
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

    // STATE CONTEXT (NEW - from StateReport)
    StateReportContext? StateContext,  // ← Nullable if state hasn't created report

    // Department data
    List<ReportDepartmentView> Departments,

    // Activity timeline
    List<ReportActivityView> ActivityLogs);
```

### Nested: StateReportContext

```csharp
public record StateReportContext(
    int StateReportId,
    string CycleLabel,
    int UnitsAttendedTo,              // How many units submitted reports
    int TotalUnitReportsCount,        // Total units in state
    int? UnitPerformanceRating,       // 0-100 score
    decimal UnitPresidentAttendanceRate, // Computed %

    // State GS commentary
    string? UnitImprovementPlan,
    string? ChallengesFaced,
    string? NationalSupportNeeded,    // Escalation notes
    string? OtherNotes,

    // Approval chain
    string? PresidentialNote,
    DateTime? ApprovedByPresidentAt,
    string? NationalNote,
    DateTime? AcknowledgedByNationalAt,

    // Aggregation metadata
    bool IsAggregated,                // Flag: has aggregation run?
    DateTime? LastAggregatedAt,       // When last aggregated?

    // Aggregated programs (by department type)
    List<StateReportProgramView> Programs);
```

---

## State-Level Aggregation Logic

### What Happens When State GS Saves Report

```
1. State GS fills state-level form:
   - Unit improvement plan
   - Challenges faced
   - National support needed
   - Unit performance rating (0-100)
   - Optional: manual program entries

2. Clicks "Save & Submit"
   └─ Component calls: CurrentUserReportService.SaveMyStateReportAsync(...)

3. Service calls: UnifiedReportService.SaveStateReportAsync()
   ├─ Authorization: CanReviewAtStateLevel(actor, stateId)
   ├─ Load StateReport + Programs
   ├─ Update: leadership commentary fields
   ├─ Call: RecomputeStateAggregateAsync()
   │   │
   │   └─ AGGREGATION ENGINE:
   │       ├─ Query all unit Reports for this state+cycle
   │       ├─ Count: UnitsAttendedTo (reports in SubmittedToState+)
   │       ├─ Count: TotalUnitReportsCount (all expected units)
   │       ├─ Calculate: UnitPerformanceRating = (attended / total) × 100
   │       ├─ Query all DepartmentReports (submitted=true) for all units
   │       ├─ Group by Department type
   │       ├─ For each department:
   │       │   ├─ Sum: AttendanceCount → TotalAttendance
   │       │   ├─ Sum: BeneficiaryCount → TotalBeneficiaries
   │       │   └─ Create StateReportProgram (IsAutoAggregated=true)
   │       ├─ Remove only old auto-aggregated programs
   │       │   (preserves manual entries)
   │       ├─ Mark: IsAggregated = true
   │       └─ Mark: LastAggregatedAt = DateTime.UtcNow
   │
   ├─ Update: StateReport.Status = SubmittedToNational (if markSubmitted=true)
   ├─ Create: ReportActivityLog("SavedStateReport", ...)
   └─ SaveChangesAsync() ← ATOMIC

4. StateReport now has:
   - Leadership commentary (manual entries)
   - Aggregated programs (from DepartmentReports)
   - Performance metrics (calculated)
   - Audit timestamp (LastAggregatedAt)
```

### Preservation Logic: Manual Entries Survive Re-Aggregation

```
Scenario: State GS adds manual program "Ramadan Camp"
├─ Creates StateReportProgram(ProgramName="Ramadan Camp", IsAutoAggregated=false)
├─ Saves

Later: More departments submit data
├─ State GS re-aggregates
├─ RecomputeStateAggregateAsync() runs:
│   ├─ Remove only rows WHERE IsAutoAggregated = true
│   │   └─ "Ramadan Camp" is NOT removed (IsAutoAggregated=false)
│   ├─ Create new auto-aggregated programs
│   │   └─ "Taleem", "Tabligh", "Welfare", etc.
│   └─ Save
└─ StateReportPrograms now contains:
    ├─ Taleem (auto, from aggregation)
    ├─ Tabligh (auto, from aggregation)
    ├─ Ramadan Camp (manual, preserved!)
    └─ etc.
```

---

## Key Design Decisions & Why

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **Service Architecture** | Unified (one service) vs. segregated | Single transaction boundary; cross-cutting auth logic; easier to test workflows |
| **State Storage** | Database only (no cache) | Reports are short-lived; multi-user concurrency; EF context is lightweight |
| **Authorization** | 3-level hierarchy + department-specific | Mirrors AMSA org structure; reduces permission explosion |
| **Report Status** | State enum + transition methods | Strong typing; clear state machine; prevents invalid transitions |
| **Aggregation** | Preserve manual entries | State GS can add state-only programs; re-aggregation doesn't erase them |
| **Cycle Management** | Deterministic (1 per calendar month) | Predictable; no ambiguity; idempotent EnsureActiveCycleAsync |
| **National Dashboard DTO** | Composite ReportWithStateContext | Single query per batch; clean data structure; solves N+1 |
| **Activity Log** | Parallel to every state change | Full audit trail; compliance requirement; immutable history |

---

## Performance Characteristics

### Query Optimization

1. **Indexed Lookups**: (UnitId, CycleId), (ReportId, Department), (StateId, CycleId)
2. **Batch Loads**: EF Include() for related collections
3. **Cache Optimization**: AmsaDirectoryLookupCache scoped (prevents repeated API calls)
4. **Token Caching**: AmsaTokenCache singleton (JWT token reuse)

### Potential Bottlenecks

1. **National Dashboard Load**: Queries all reports + all state reports
   - Mitigation: Pagination (not currently implemented)
   - Safe for now: ~100 units × 1-2 active cycles = ~200 reports/cycle

2. **Aggregation Engine**: Queries all DepartmentReports per state
   - Acceptable: Runs on-demand during StateReport save
   - Could be async if slow (background job pattern)

3. **AMSA API Integration**: External dependency
   - Mitigation: AmsaTokenCache (singleton) + AmsaDirectoryLookupCache (scoped)
   - Fallback: Display IDs if lookup fails

---

## Testing Strategy

### Unit Test Targets

1. **ReportAccessService**: Role matrix validation
2. **State Transitions**: Precondition checks (SaveDepartmentDataAsync, ApproveByUnitLeadershipAsync, etc.)
3. **Aggregation Logic**: Sum calculations, preservation of manual entries
4. **DTO Mapping**: Composite DTO creation

### Integration Test Targets

1. **Full Workflow**: Submit dept → Unit approves → State approves → National acknowledges
2. **Authorization Checks**: Unauthorized users blocked at each level
3. **Cycle Locking**: Deadline enforcement
4. **Aggregation Triggers**: State GS saves report → StateReportPrograms created

### Component Test Targets (Blazor)

1. **Dashboard Routing**: Role-based dashboard selection
2. **Form Submission**: Component → Service call → UI update
3. **Error Handling**: Show error messages on auth failures, validation errors

---

## Migration & Deployment Notes

### Database Migrations

- Uses EF Core migrations (in `/Migrations` folder)
- Seeded data: January 2025 cycle
- Key migrations:
  - `20260423163152_ConsolidateDepartmentReports`: Hybrid model for departments
  - `20260505104852_AddStateReportDepartmentDataLinkage`: State report structure
  - `20260510163129_EnforceStateAndNationalAggregationCoreModel`: Aggregation fields
  - `20260513111813_AddStateAggregationMetadata`: Metadata (IsAggregated, LastAggregatedAt)

### Deployment Checklist

- [ ] SQL Server database (LocalDB or Azure SQL)
- [ ] AMSA API credentials (base URL, app credentials)
- [ ] Azure AD / authentication provider configured
- [ ] Connection string in `appsettings.json` or Azure Key Vault
- [ ] Run `dotnet ef database update`
- [ ] Build + publish Blazor server + WASM projects
- [ ] Test authentication flow
- [ ] Verify AMSA API connectivity (member lookup)

---

## Summary: The Big Picture

```
AMSA Reporting System is a MULTI-LEVEL HIERARCHICAL workflow engine for:

1. MONTHLY COMPLIANCE REPORTING (100+ units, 9 departments each)
   └─ Monthly period (ReportingCycle) with submission deadline

2. DEPARTMENT-LEVEL SUBMISSION (DepartmentReport per department per unit)
   └─ JSON payload (flexible schema) + denormalized analytics fields

3. UNIT-LEVEL AGGREGATION (Report combines all 9 departments)
   └─ Status: Draft → SubmittedToPresident → SubmittedToState → SubmittedToNational → Acknowledged

4. APPROVAL WORKFLOW (3 leadership levels)
   ├─ Unit President approves (SubmittedToPresident → SubmittedToState)
   ├─ State GS approves + aggregates (SubmittedToState → SubmittedToNational)
   └─ National leadership acknowledges (SubmittedToNational → Acknowledged)

5. STATE-LEVEL AGGREGATION (StateReport combines all units + state commentary)
   ├─ Metrics: UnitsAttendedTo, UnitPerformanceRating
   ├─ Commentary: UnitImprovementPlan, ChallengesFaced, NationalSupportNeeded
   └─ Programs: Auto-aggregated from DepartmentReports (with manual entry preservation)

6. NATIONAL VISIBILITY (Dashboard shows all reports + state context)
   └─ Composite DTO: ReportWithStateContext (unit report + state report side-by-side)

BUILT ON:
- ASP.NET Core (backend)
- Blazor Server + WebAssembly (frontend)
- Entity Framework Core (data access)
- SQL Server (persistence)
- AMSA REST API (external data)
- JWT tokens (authentication)

CORE PRINCIPLES:
✓ Database as source of truth (no redundant caching)
✓ Atomic transactions (SaveChangesAsync = one unit of work)
✓ Audit trail first class (every state change logged)
✓ Role-based hierarchical access control (mirrors org structure)
✓ Strong typing over strings (enums for status, departments, levels)
✓ Async/await throughout (all I/O is non-blocking)
```

---

**Generated**: January 2025
**System**: AMSA Reporting System
**Architecture**: Blazor + EF Core + SQL Server
**Target Framework**: .NET 10, C# 14
