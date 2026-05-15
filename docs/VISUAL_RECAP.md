# SYSTEM ANALYSIS - VISUAL RECAP

## The AMSA Reporting System at a Glance

### 🏢 What It Does

```
Monthly Compliance Reporting Platform for AMSA Organization
│
├─ 100+ Units across multiple states
├─ 9 departments per unit (Taleem, Tabligh, Welfare, Sport, Finance, Health, etc.)
├─ 3 leadership levels (Unit → State → National)
└─ Monthly submission cycles with deadline enforcement
```

---

## 📊 Core Data Model (5 Tables)

```
┌──────────────────────────────────────────────────────────────────┐
│ ReportingCycle (Monthly Period)                                  │
│ - CycleMonth: "January 2025"                                     │
│ - StartDate, EndDate, SubmissionDeadline                         │
│ - IsLocked: true/false (auto-locks after deadline)              │
└──────────────────────┬───────────────────────────────────────────┘
                       │ (1) ─────< (many)
                       │
        ┌──────────────┴──────────────┐
        │                             │
        ▼                             ▼
┌─────────────────────┐      ┌──────────────────────┐
│ Report              │      │ StateReport          │
│ (Unit Report)       │      │ (Aggregation)        │
│                     │      │                      │
│ UnitId + CycleId    │      │ StateId + CycleId    │
│ Status: [workflow]  │      │ Status: [workflow]   │
│ CreatedAt, Updated  │      │ UnitsAttendedTo      │
│ SubmittedAt...      │      │ TotalUnitReports     │
│ ApprovedAt (3 x)    │      │ UnitPerformance (%)  │
│ AcknowledgedAt      │      │                      │
└──────────┬──────────┘      └─────────┬────────────┘
           │ (1) ─────< (many)        │ (1) ─────< (many)
           │                          │
           ▼                          ▼
┌──────────────────────────────────────┐  ┌─────────────────────┐
│ DepartmentReport (9 per Report)      │  │ StateReportProgram  │
│                                      │  │ (Auto-aggregated)   │
│ ReportId + Department                │  │                     │
│ IsSubmitted: true/false              │  │ ProgramName         │
│ SubmittedAt, SubmittedByMemberId    │  │ TotalAttendance     │
│ ReportData: JSON (flexible schema)   │  │ IsAutoAggregated    │
│ Extracted fields:                    │  │ (flag for preserve) │
│   - AttendanceCount                  │  │                     │
│   - SessionsOrganized                │  │                     │
│   - DuesCollected                    │  │                     │
│   - BeneficiaryCount, etc.          │  │                     │
└──────────────────────────────────────┘  └─────────────────────┘

(Also: ReportActivityLog - audit trail)
```

---

## 🔄 Report Status Workflow

```
                    ┌─── Reject ───────┐
                    │                  ▼
         ┌─ DRAFT ◄─┴─ REJECTED_BY_PRESIDENT
         │
         │ Submit (≥1 dept)
         ▼
    SUBMITTED_TO_PRESIDENT
         │
         ├─ Approve ──────────┐
         │                    │ Unit President
         │                    ▼
         │            SUBMITTED_TO_STATE
         │                    │
         │                    ├─ Approve ─────────┐
         │                    │                   │ State GS
         │                    │                   ▼
         │                    │           SUBMITTED_TO_NATIONAL
         │                    │                   │
         │                    │                   ├─ Acknowledge ┐
         │                    │                   │              │ National
         │                    │                   │              ▼
         │                    │                   │          ACKNOWLEDGED [FINAL]
         │                    │                   │
         │                    │                   └─ Reject ─┐
         │                    │                              │
         │                    └─ Reject ──────────┐          │
         │                                        ▼          │
         └────────────────────────────────────────────────────┘
            (Any rejection → back to DRAFT)
```

---

## 👥 Authorization Hierarchy

```
┌─────────────────────────────────────────────────────────────┐
│ NATIONAL LEADERSHIP (LevelType.National)                    │
│ ├─ President                                                │
│ ├─ General Secretary                                        │
│ ├─ VPs (Admin, North, South-Southwest, Southwest)         │
│ └─ National department heads                               │
│    └─ Can edit/approve/acknowledge ANY report             │
│       └─ HasSudoAccess = true                              │
├─────────────────────────────────────────────────────────────┤
│ STATE LEADERSHIP (LevelType.State)                          │
│ ├─ State General Secretary (GS)                            │
│ ├─ State department heads                                  │
│ └─ State officers                                          │
│    └─ Can edit/approve reports in their STATE only        │
├─────────────────────────────────────────────────────────────┤
│ UNIT LEADERSHIP (LevelType.Unit)                            │
│ ├─ Unit President                                          │
│ ├─ Department officers                                     │
│ └─ Members                                                 │
│    └─ Can edit/submit reports in their UNIT only          │
└─────────────────────────────────────────────────────────────┘

ROLE FORMAT: "DepartmentName:LevelType"
Examples: "Taleem:Unit", "Finance:State", "General:National"

PERMISSION RULE:
   CanEditDepartment(actor, unitId, stateId, department)
   ├─ National leadership? → YES
   ├─ State leader (this state)? → YES
   ├─ Unit leader (this unit)? → YES
   ├─ Department officer (this dept, this unit)? → YES
   ├─ Department officer (this dept, this state)? → YES
   ├─ National dept head? → YES
   └─ Else → NO
```

---

## 🔗 Module Integration Map

```
┌──────────────────────────────────────────────────────────────┐
│ PRESENTATION LAYER (Blazor Components)                       │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  Dashboard Router selects role-based component:             │
│  ├─ NationalLeadershipDashboard (national view)             │
│  ├─ StateLeadershipDashboard (state view)                   │
│  ├─ UnitLeadershipDashboard (unit view)                     │
│  └─ DepartmentOfficerDashboard (form editing)               │
│                                                              │
│  Each component injects: CurrentUserReportService           │
│                                                              │
└────────────────┬─────────────────────────────────────────────┘
                 │
                 │ Scoped: CurrentUserReportService
                 │  └─ Extracts user context automatically
                 │
┌────────────────▼─────────────────────────────────────────────┐
│ SERVICE LAYER (Business Logic)                              │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  UnifiedReportService (core logic):                         │
│  ├─ SaveDepartmentDataAsync() ← save dept forms             │
│  ├─ SaveStateReportAsync() ← save state report + aggregate  │
│  ├─ SubmitReportToPresidentAsync() ← state transition       │
│  ├─ ApproveByUnitLeadershipAsync() ← approval chain         │
│  ├─ ApproveByStateLeadershipAsync() ← approval chain        │
│  ├─ AcknowledgeByNationalAsync() ← final acknowledgment     │
│  ├─ GetNationalReportsWithStateContextAsync() ← composite DTO
│  ├─ EnsureActiveCycleAsync() ← monthly cycle mgmt           │
│  └─ RecomputeStateAggregateAsync() ← auto-aggregation       │
│                                                              │
│  ReportAccessService (authorization):                       │
│  ├─ CanEditDepartment()                                     │
│  ├─ CanReviewAtUnitLevel()                                  │
│  ├─ CanReviewAtStateLevel()                                 │
│  ├─ IsNationalLeadership()                                  │
│  └─ [Role parsing logic]                                    │
│                                                              │
│  Supporting services:                                       │
│  ├─ AmsaAuthService (member lookup, JWT)                    │
│  ├─ AMSAAuthStateProvider (cascading auth state)            │
│  ├─ AmsaDirectoryLookupCache (lookup optimization)          │
│  └─ AmsaApiClient (external API integration)                │
│                                                              │
└────────────────┬─────────────────────────────────────────────┘
                 │
                 │ EF Core DbContext queries
                 │
┌────────────────▼─────────────────────────────────────────────┐
│ DATA ACCESS LAYER (Entity Framework Core)                   │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  AMSAReportingDbContext:                                    │
│  ├─ DbSet<ReportingCycle>                                   │
│  ├─ DbSet<Report>                                           │
│  ├─ DbSet<DepartmentReport>                                 │
│  ├─ DbSet<StateReport>                                      │
│  ├─ DbSet<StateReportProgram>                               │
│  └─ DbSet<ReportActivityLog>                                │
│                                                              │
└────────────────┬─────────────────────────────────────────────┘
                 │
                 │ SQL Server queries
                 │
┌────────────────▼─────────────────────────────────────────────┐
│ DATABASE LAYER (SQL Server)                                 │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  Tables (with indices & constraints):                       │
│  ├─ ReportingCycles (1-2 active)                            │
│  ├─ Reports (100-200 per cycle)                             │
│  ├─ DepartmentReports (900 per cycle: 9 depts × 100 units) │
│  ├─ StateReports (50 per cycle)                             │
│  ├─ StateReportPrograms (auto-aggregated)                   │
│  └─ ReportActivityLogs (audit trail, grows indefinitely)    │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

---

## 💾 State Persistence Flow

```
USER ACTION (fill form, click save)
    │
    ▼
COMPONENT
    │ Calls: CurrentUserReportService.SaveDepartmentJsonAsync()
    │
    ▼
SERVICE (gets current user context)
    │
    ├─ 1. Auth check: ReportAccessService.CanEditDepartment()
    │    └─ Throw if unauthorized
    │
    ├─ 2. Load entity: _db.DepartmentReports.FirstOrDefaultAsync()
    │
    ├─ 3. Validate: JSON syntax, preconditions
    │    └─ Throw if invalid
    │
    ├─ 4. Mutate in memory:
    │    ├─ ReportData = json
    │    ├─ IsSubmitted = true
    │    ├─ SubmittedAt = DateTime.UtcNow
    │    └─ Updated analytics fields
    │
    ├─ 5. Audit log:
    │    └─ _db.ReportActivityLogs.Add(new ReportActivityLog(...))
    │
    └─ 6. ATOMIC TRANSACTION:
        └─ await _db.SaveChangesAsync()
            │
            ▼
        EF CORE
            │ Generates SQL
            │
            ▼
        SQL SERVER
            │
            ├─ BEGIN TRANSACTION
            ├─ UPDATE DepartmentReports SET ...
            ├─ INSERT INTO ReportActivityLogs ...
            └─ COMMIT (all-or-nothing)
            │
            ▼ [If error: ROLLBACK]

        Return updated entity
            │
            ▼
COMPONENT receives response
    │
    ├─ Update local state
    └─ Re-render UI
```

---

## 📈 Aggregation Engine (State Report Auto-Aggregation)

```
State GS clicks "Save & Submit State Report"
    │
    ▼
StateLeadershipDashboard component
    │ Calls: CurrentUserReportService.SaveMyStateReportAsync(...)
    │
    ▼
UnifiedReportService.SaveStateReportAsync(actor, stateReportId, form, markSubmitted=true)
    │
    ├─ Load StateReport + Programs
    ├─ Update: UnitImprovementPlan, ChallengesFaced, etc.
    │
    ├─ Call: RecomputeStateAggregateAsync(stateReport)
    │  │
    │  └─ AGGREGATION ENGINE:
    │     │
    │     ├─ Query all Reports for state+cycle
    │     │  └─ Count units with status ≥ SubmittedToState
    │     │  └─ Result: UnitsAttendedTo
    │     │
    │     ├─ Calculate: UnitPerformanceRating = (UnitsAttendedTo / Total) × 100
    │     │
    │     ├─ Query all DepartmentReports (IsSubmitted=true)
    │     │  └─ Group by Department type (Taleem, Tabligh, etc.)
    │     │
    │     ├─ For EACH department group:
    │     │  ├─ SUM AttendanceCount → TotalAttendance
    │     │  ├─ SUM BeneficiaryCount → TotalBeneficiaries
    │     │  └─ Create StateReportProgram(IsAutoAggregated=true)
    │     │
    │     ├─ DELETE old programs WHERE IsAutoAggregated=true
    │     │  └─ Preserves manual entries
    │     │
    │     └─ Mark: IsAggregated=true, LastAggregatedAt=now
    │
    ├─ If markSubmitted=true:
    │  └─ StateReport.Status = SubmittedToNational
    │
    ├─ Add audit log: ReportActivityLog(...)
    │
    └─ SaveChangesAsync() ← ATOMIC

StateReport now contains:
├─ Leadership commentary (manual)
├─ Aggregated programs (auto)
├─ Performance metrics (calculated)
└─ Timestamp (LastAggregatedAt)
```

---

## 🎯 Composite DTO for National Dashboard

```
National Leadership Opens NationalLeadershipDashboard
    │
    ▼
Component calls: GetNationalReportsWithStateContextAsync()
    │
    ▼
Service logic:
├─ Auth: IsNationalLeadership(actor) ✓
├─ Query: All Reports for cycle (with includes)
│
└─ For EACH Report:
   │
   ├─ Load StateReport for same state+cycle
   │
   ├─ Map to DTOs:
   │  ├─ Report → ReportDepartmentView[]
   │  ├─ StateReport → StateReportContext (or null)
   │  ├─ ActivityLogs → ReportActivityView[]
   │  └─ Create ReportWithStateContext (COMPOSITE)
   │
   └─ Return List<ReportWithStateContext>

Component receives:
├─ ReportWithStateContext[]:
│  ├─ ReportId, UnitId, StateId
│  ├─ Status, timestamps
│  ├─ StateContext ← nested StateReport data (or null)
│  │  ├─ UnitsAttendedTo / TotalUnitReportsCount
│  │  ├─ UnitPerformanceRating
│  │  ├─ Leadership commentary
│  │  └─ Aggregated programs
│  ├─ Departments ← 9 department entries
│  └─ ActivityLogs ← audit timeline

Component renders table:
├─ One row per unit report
├─ Columns: State, Unit, Status, UnitsAttended, Rating, Actions
└─ Expandable: See department breakdown + activity timeline
```

---

## 🔐 Authorization Decision Tree

```
Can this user edit the Taleem department report for Unit 5?
    │
    ├─ Is user national leadership?
    │  └─ YES → Allow
    │  └─ NO → Next check
    │
    ├─ Is user state leader AND in the same state as Unit 5?
    │  └─ YES → Allow
    │  └─ NO → Next check
    │
    ├─ Is user unit leader AND in Unit 5?
    │  └─ YES → Allow
    │  └─ NO → Next check
    │
    ├─ Is user a Taleem officer AND in Unit 5?
    │  └─ YES → Allow
    │  └─ NO → Next check
    │
    ├─ Is user a Taleem officer AND at state level (same state as Unit 5)?
    │  └─ YES → Allow
    │  └─ NO → Next check
    │
    ├─ Is user a national Taleem officer?
    │  └─ YES → Allow
    │  └─ NO → Deny
    │
    └─ Throw: UnauthorizedAccessException
```

---

## 📊 Key Numbers

| Metric | Value | Notes |
|--------|-------|-------|
| **Departments per Unit** | 9 | Fixed (Taleem, Tabligh, Welfare, etc.) |
| **States** | ~50 | (Estimated) |
| **Units per State** | ~2-4 | (Varies) |
| **Total Units** | ~100-200 | (Organization size) |
| **Reports per Cycle** | ~100-200 | One per unit |
| **DepartmentReports per Cycle** | ~900 | 9 × ~100 |
| **StateReports per Cycle** | ~50 | One per state |
| **Active Cycles** | 1-2 | Usually current month |
| **Approval Levels** | 3 | Unit → State → National |
| **Role Types** | 3+ | Unit, State, National + departments |

---

## ✅ System Principles

```
1. DATABASE IS SOURCE OF TRUTH
   └─ No redundant caching; every read hits database
   └─ Exception: AmsaTokenCache (JWT refresh optimization)

2. ATOMIC TRANSACTIONS
   └─ Every state change = one SaveChangesAsync()
   └─ All-or-nothing: success or rollback

3. STRONG TYPING
   └─ Enums for status, departments, levels
   └─ Avoid magic strings

4. AUDIT TRAIL FIRST CLASS
   └─ Every action logged to ReportActivityLog
   └─ Immutable history for compliance

5. ROLE-BASED HIERARCHICAL RBAC
   └─ 3 levels: Unit, State, National
   └─ Department-specific permissions
   └─ Mirrors organization structure

6. FAIL-FAST VALIDATION
   └─ Authorization checked first
   └─ Preconditions validated before mutation
   └─ Exceptions thrown early

7. STATE MACHINE DISCIPLINE
   └─ Report status follows explicit workflow
   └─ Invalid transitions rejected
   └─ State history preserved in audit trail

8. ASYNC/AWAIT THROUGHOUT
   └─ All I/O is non-blocking
   └─ Proper CancellationToken support
   └─ Scalable for concurrent users
```

---

## 🚀 Data Flow Summary

```
┌────────────────────────────────────────────────────────────────┐
│ 1. SUBMISSION PHASE                                            │
├────────────────────────────────────────────────────────────────┤
│ Department officer fills form → SaveDepartmentJsonAsync()     │
│ → AuthCheck → LoadEntity → ValidateJSON → MutateInMemory      │
│ → AddAuditLog → SaveChangesAsync() [ATOMIC]                   │
│ → Component receives updated DepartmentReport                 │
└────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────┐
│ 2. UNIT APPROVAL PHASE                                         │
├────────────────────────────────────────────────────────────────┤
│ Unit president reviews all 9 depts → ApproveAtUnitAsync()     │
│ → AuthCheck → ValidateStatus → CheckConstraints               │
│ → UpdateStatus(SubmittedToState) → AddAuditLog                │
│ → SaveChangesAsync() [ATOMIC]                                 │
│ → Report transitions: SubmittedToPresident → SubmittedToState │
└────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────┐
│ 3. STATE AGGREGATION PHASE                                     │
├────────────────────────────────────────────────────────────────┤
│ State GS fills state form → SaveMyStateReportAsync()          │
│ → AuthCheck → LoadStateReport → UpdateCommentary              │
│ → RecomputeStateAggregateAsync():                             │
│    - Query all unit Reports (this state+cycle)                │
│    - Count submitted-or-higher → UnitsAttendedTo              │
│    - Query all DepartmentReports → Group by Dept              │
│    - SUM metrics → Create StateReportPrograms                 │
│    - Delete old auto-aggregated rows (preserve manual)        │
│    - Mark: IsAggregated=true, LastAggregatedAt=now            │
│ → UpdateStatus(SubmittedToNational) if markSubmitted          │
│ → AddAuditLog → SaveChangesAsync() [ATOMIC]                   │
└────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────┐
│ 4. NATIONAL REVIEW PHASE                                       │
├────────────────────────────────────────────────────────────────┤
│ National leadership opens dashboard                            │
│ → GetNationalReportsWithStateContextAsync()                   │
│ → AuthCheck → QueryAllReports + StateReports (per unit)       │
│ → MapToCompositeDTO: ReportWithStateContext (includes         │
│    StateReportContext, Departments, ActivityLogs)             │
│ → Component renders table (state context visible)             │
│ → National clicks "Acknowledge" → AcknowledgeAtNationalAsync()│
│ → UpdateStatus(Acknowledged) → AddAuditLog                    │
│ → SaveChangesAsync() [ATOMIC] [FINAL STATE]                   │
└────────────────────────────────────────────────────────────────┘

[If rejected at any level: Status → RejectedByXxx → back to Draft]
```

---

**END OF VISUAL RECAP**

This document provides a high-level overview of the system architecture, state management, authorization, and data flow patterns. Use it for quick reference, onboarding, and architectural discussions.
