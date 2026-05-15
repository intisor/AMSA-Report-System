# AMSA System - Quick Reference Card

## System Type
**Blazor Server + WebAssembly | .NET 10 | EF Core | SQL Server**

---

## What Does It Do?
Multi-level hierarchical reporting platform for monthly compliance across 100+ units, 9 departments, 3 leadership levels.

---

## 5 Core Tables

| Table | Purpose | Key Constraint | Count |
|-------|---------|---|---|
| **ReportingCycle** | Monthly periods | CycleMonth (unique) | 1-2 active |
| **Report** | Unit aggregation | (UnitId, CycleId) | ~100-200/cycle |
| **DepartmentReport** | Dept entry (JSON) | (ReportId, Dept) | ~900/cycle |
| **StateReport** | State aggregation | (StateId, CycleId) | ~50/cycle |
| **StateReportProgram** | Auto-aggregated | StateReportId | ~400/cycle |

---

## Report Status Machine (Workflow)

```
Draft
  ├─ Submit → SubmittedToPresident
  │   ├─ Approve → SubmittedToState
  │   │   ├─ Approve → SubmittedToNational
  │   │   │   ├─ Acknowledge → Acknowledged [FINAL]
  │   │   │   └─ Reject → (back to Draft)
  │   │   └─ Reject → (back to Draft)
  │   └─ Reject → (back to Draft)
  └─ [Any rejection → RejectedByXxx → back to Draft]
```

---

## Authorization (3 Levels + Department-Specific)

```
National > State > Unit
├─ National: Any level (HasSudoAccess)
├─ State: Same StateId + StateLevel role
├─ Unit: Same UnitId + UnitLevel role
└─ Department: Same DepartmentName + any level + same geo
```

---

## Key Services

| Service | Purpose | Scope |
|---------|---------|-------|
| **UnifiedReportService** | Core business logic (700+ LOC) | Scoped |
| **CurrentUserReportService** | Wrapper (auto user extraction) | Scoped |
| **ReportAccessService** | Authorization checks | Transient |
| **AmsaAuthService** | Member lookup + JWT | Scoped |
| **AmsaTokenCache** | JWT caching | **Singleton** |

---

## Main Methods (CurrentUserReportService)

```csharp
// Submit dept
SaveDepartmentJsonAsync(reportId, dept, json, markSubmitted)

// Submit/approve/reject
SubmitReportToPresidentAsync(reportId, notes)
ApproveAtUnitAsync(reportId, notes)
ApproveAtStateAsync(reportId, notes)
AcknowledgeAtNationalAsync(reportId, notes)

// State report (triggers aggregation)
SaveMyStateReportAsync(stateReportId, form, markSubmitted)

// National dashboard (composite view)
GetNationalReportsWithStateContextAsync(cycleId)
```

---

## State Persistence Pattern

```
1. Component calls service
2. Service authorizes (throw if denied)
3. Service loads entity from DB
4. Service validates preconditions (throw if invalid)
5. Service mutates in memory
6. Service adds audit log
7. Service calls SaveChangesAsync() ← ATOMIC
8. Component re-renders with new data
```

---

## Aggregation Trigger

**When**: State GS saves state report form with `markSubmitted=true`

**What**: RecomputeStateAggregateAsync() runs:
- Query all unit Reports (submitted-or-higher count)
- Query all DepartmentReports (group by type)
- SUM AttendanceCount, BeneficiaryCount per department
- Create StateReportPrograms (IsAutoAggregated=true)
- Delete old auto-aggregated rows (preserve manual entries)
- Mark: IsAggregated=true, LastAggregatedAt=now

---

## Composite DTO (ReportWithStateContext)

**Used by**: National Dashboard

**Contains**:
- Unit Report data (status, timestamps, notes)
- **StateContext** ← StateReport (nested, nullable)
  - UnitsAttendedTo, UnitPerformanceRating
  - Leadership commentary
  - Aggregated programs
- Department list (9 entries)
- Activity log (audit trail)

**Why**: National sees unit + state context in one view (no N+1)

---

## Authorization Checks (Where & How)

```csharp
// All methods start with:
if (!_access.CanXxx(actor, params))
    throw new UnauthorizedAccessException(...);

// Examples:
CanEditDepartment(actor, unitId, stateId, department)
CanReviewAtUnitLevel(actor, unitId)
CanReviewAtStateLevel(actor, stateId)
IsNationalLeadership(actor)
```

---

## Error Handling

| Exception | When | Fix |
|-----------|------|-----|
| **UnauthorizedAccessException** | Auth fails | Check user role |
| **InvalidOperationException** | Business rule violates | Check preconditions |
| **ArgumentException** | Invalid input (JSON, etc.) | Validate input |

All caught in component:
```csharp
try { await service.Method(...); }
catch (Exception ex) { SetMessage(ex.Message, false); }
```

---

## Audit Trail Structure

```csharp
ReportActivityLog
├─ ReportId (which report)
├─ ActionByMemberId (who did it)
├─ Action (action type string)
├─ ActionAt (when)
└─ Notes (optional: reason, feedback)
```

**Action types**: DepartmentReportSaved, DepartmentReportSubmitted, ReportCreated, ApprovedByUnitLeadership, RejectedByStateLeadership, AcknowledgedByNational, etc.

---

## Cycle Management

```csharp
// Create/get current month cycle
await EnsureActiveCycleAsync()
// Returns: ReportingCycle for Jan 2025 (auto-created if missing)

// Check if cycle open before edits
await EnsureCycleOpenForEditsAsync(cycleId)
// Auto-locks if past deadline; throws if locked
```

---

## Dashboard Routing

```
Dashboard.razor (Router)
  ├─ @if (user.IsNationalLeadership) → NationalLeadershipDashboard
  ├─ @if (user.IsStateLeadership) → StateLeadershipDashboard
  ├─ @if (user.IsUnitLeadership) → UnitLeadershipDashboard
  └─ @else → DepartmentOfficerDashboard
```

---

## Database Indices

```
Report: (UnitId, CycleId) [unique]
        Status, UnitId, StateId, CycleId [individual]

DepartmentReport: (ReportId, Department) [unique]
                  ReportId, CycleId

StateReport: (StateId, CycleId) [unique]
             Status, StateId, CycleId

StateReportProgram: StateReportId

ReportActivityLog: ReportId
```

---

## Common Queries

```csharp
// Get all reports for a unit in current cycle
var reports = await service.GetMyUnitReportsAsync(cycleId: null)

// Get unit's history (all cycles)
var history = await service.GetCurrentUserReportHistoryAsync()

// Get state's reports (all units)
var stateReports = await service.GetMyStateReportsAsync(cycleId)

// Get national view (with state context)
var national = await service.GetNationalReportsWithStateContextAsync(cycleId)

// Check if user can edit Taleem dept
bool canEdit = service.CanEditOwnUnitDepartment(DepartmentType.Taleem)
```

---

## Validation Rules

**Before submit**: ≥1 department submitted

**Before state save**: UnitPresidentsAttended ≤ TotalUnitPresidents, rating 0-100

**JSON validation**: Syntax check via JsonDocument.Parse()

**Cycle check**: EnsureCycleOpenForEditsAsync() (auto-locks past deadline)

---

## Performance Notes

| Operation | Complexity | Notes |
|-----------|-----------|-------|
| SaveDepartment | O(1) | Single entity + log |
| GetNationalReports | O(U+S) | U=units, S=states; batch load |
| RecomputeAggregate | O(D) | D=DepartmentReports (scanned once) |
| AuthCheck | O(R) | R=parsed roles (typically <5) |

---

## Environment Setup

```csharp
// appsettings.json
{
  "ConnectionStrings": {
    "AMSAReportingDb": "Server=...;Database=AMSAReportingDb;..."
  },
  "AmsaApiClientOptions": {
    "BaseUrl": "https://amsa-api.example.com",
    "AppId": "...",
    "AppSecret": "..."
  }
}

// Program.cs
builder.Services.AddDbContext<AMSAReportingDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddScoped<UnifiedReportService>();
builder.Services.AddScoped<CurrentUserReportService>();
// ... etc
```

---

## Deployment Checklist

- [ ] SQL Server (LocalDB or Azure SQL)
- [ ] Connection string configured
- [ ] `dotnet ef database update`
- [ ] AMSA API credentials
- [ ] Azure AD configured
- [ ] Build + publish
- [ ] Test auth flow
- [ ] Test AMSA API connectivity

---

## Key Constraints

| Constraint | Reason |
|-----------|---------|
| (UnitId, CycleId) unique on Report | One report per unit per cycle |
| (ReportId, Department) unique on DepartmentReport | One dept entry per report |
| (StateId, CycleId) unique on StateReport | One state report per cycle |
| CycleMonth unique on ReportingCycle | One cycle per month |
| Restrict delete on Cycle | Historical record protection |
| Cascade delete Report → DepartmentReports | Data integrity |

---

## Testing Strategy

**Unit Tests**:
- ReportAccessService (role matrix)
- State transitions (preconditions)
- Aggregation logic (sums)

**Integration Tests**:
- Full workflow (submit → approve → acknowledge)
- Authorization at each level
- Cycle locking

**Component Tests**:
- Dashboard routing
- Form submission
- Error messages

---

## Code Quality

- ✅ Strong typing (enums)
- ✅ No magic strings
- ✅ Async/await throughout
- ✅ CancellationToken support
- ✅ Atomic transactions
- ✅ Audit trail
- ✅ RBAC discipline
- ✅ Fail-fast validation

---

## Gotchas & Antipatterns

❌ **Don't**: Cache reports in service
❌ **Don't**: Skip authorization checks
❌ **Don't**: Share DbContext across requests
❌ **Don't**: Assume state reports exist
❌ **Don't**: Manually construct role strings

✅ **Do**: Always validate preconditions
✅ **Do**: Use atomic transactions
✅ **Do**: Check authorization first
✅ **Do**: Handle NullReferenceException (nullable StateContext)
✅ **Do**: Log all actions

---

## Useful Aliases

```csharp
using ReportStatus = AMSAReportingSystem.Data.Entities.ReportStatus;
using DepartmentType = AMSAReportingSystem.Data.Entities.DepartmentType;
using LevelType = AMSAReportingSystem.Data.Entities.LevelType;
```

---

**Last Updated**: January 2025  
**System**: AMSA Reporting System  
**Target**: .NET 10, Blazor Server + WASM, EF Core 10, SQL Server  
**Status**: Production-ready (as of migration analysis)
