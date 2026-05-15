# AMSA Reporting System - Blazor Architecture & Component Flow

## 1. ARCHITECTURE OVERVIEW

The system is built on **Blazor Server (Interactive Server Render Mode)** with a multi-tier authorization structure: **National → State → Unit → Department**.

### Entry Point Flow
```
App.razor (root)
    ↓
Routes.razor (auto-routing via @page directives)
    ↓
MainLayout.razor (wraps all pages)
    ↓
Dashboard.razor (hub - routes to role-specific dashboards)
```

---

## 2. COMPONENT HIERARCHY & ROUTING

### **Root Components**
| Component | Route | Purpose |
|-----------|-------|---------|
| **App.razor** | N/A | Entry point, loads CSS/JS, renders `<Routes />` |
| **Routes.razor** | N/A | Discovers all `@page` directives, sets default layout |
| **MainLayout.razor** | N/A | Wraps all pages with NavMenu + ReconnectModal |

### **Page Components (with @page routes)**
| Component | Route | Purpose |
|-----------|-------|---------|
| **Login.razor** | `/login` | Authentication entry |
| **Dashboard.razor** | `/dashboard` | Central hub - conditionally renders role dashboards |
| **DepartmentReportEditor.razor** | `/report/{DepartmentSlug}` | Form editor for departments (Taleem, Tabligh, etc.) |
| **Error.razor** | (implicit) | Error display |
| **NotFound.razor** | (implicit) | 404 handling |
| **Home.razor** | `/` | Redirects to /dashboard |

### **Nested Dashboard Components (child of Dashboard.razor)**
| Component | Role | Used By |
|-----------|------|---------|
| **DepartmentOfficerDashboard.razor** | Department officers in units | Dashboard.razor |
| **UnitPresidentDashboard.razor** | Unit leaders | Dashboard.razor |
| **StateGSDashboard.razor** | State leadership (GS) | Dashboard.razor |
| **NationalDashboard.razor** | National leadership | Dashboard.razor |

---

## 3. DASHBOARD ROUTING LOGIC (Dashboard.razor)

```csharp
@code {
    private AuthContext? CurrentUser;

    protected override async Task OnInitializedAsync()
    {
        var amsaProvider = (AMSAAuthStateProvider)AuthStateProvider;
        CurrentUser = amsaProvider.GetCurrentUser();
        if (CurrentUser == null)
            Nav.NavigateTo("/login", forceLoad: true);
    }
}
```

**Conditional Rendering Logic:**
```
CurrentUser → Check Role Hierarchy
    ↓
CurrentUser.GetDashboard() == "NationalDashboard" → <NationalDashboard />
    ↓ (else if)
CurrentUser.GetDashboard() == "StateDashboard" → <StateGSDashboard />
    ↓ (else if)
CurrentUser.IsUnitLeadership == true → <UnitPresidentDashboard />
    ↓ (else if)
CurrentUser.GetDashboard() == "UnitDashboard" → <DepartmentOfficerDashboard />
    ↓ (else)
"Your role is not configured" → Alert
```

**Key Issue #1**: Multiple ways to check roles:
- `CurrentUser.GetDashboard()` (returns string)
- `CurrentUser.IsUnitLeadership` (boolean property)
- `CurrentUser.IsNationalLeadership` (boolean property)
- `CurrentUser.IsStateLeadership` (boolean property)
- `CurrentUser.ParsedRoles` (list of roles)

→ **Inconsistent. Recommend using a single `Role` enum.**

---

## 4. DATA FLOW: Department Officer (Lowest Level)

```
DepartmentOfficerDashboard.razor
    ├─ Injects: CurrentUserReportService
    ├─ On Load:
    │   ├─ EnsureActiveCycleAsync()  ← Gets/creates current cycle
    │   ├─ GetCurrentUserDraftAsync() ← Gets user's draft for cycle (or NULL)
    │   └─ Conditional Render States:
    │       ├─ NoReportExists → "Create New Report" button
    │       ├─ DraftExists → Show report editor link
    │       ├─ Rejected → Show rejection message
    │       ├─ SubmittedToPresident → Show "submitted" status
    │       └─ Acknowledged → Show completion
    └─ Actions:
        ├─ CreateCurrentUserDraftAsync(cycleId)
        │   └─ Navigates to /report/{DepartmentSlug}
        └─ [Dashboard state machine drives UI]

        ↓

/report/{DepartmentSlug} → DepartmentReportEditor.razor
    ├─ Extract DepartmentSlug from URL (e.g., "taleem" → DepartmentType.Taleem)
    ├─ Injects: CurrentUserReportService
    ├─ On Load:
    │   ├─ Get active cycle
    │   ├─ GetCurrentUserDraftAsync(cycleId) ← Fetch draft (must exist)
    │   ├─ GetDepartmentAsync(reportId, department) ← Fetch dept-specific data
    │   ├─ LoadDepartmentForm(reportData) ← Parse JSON → bind to form model
    │   └─ Display department-specific questions (Taleem, Tabligh, Welfare, etc.)
    ├─ Form State:
    │   ├─ Draft: input enabled
    │   ├─ RejectedByPresident/State: editable (revision)
    │   └─ SubmittedToPresident/National/Acknowledged: read-only
    └─ Actions:
        ├─ SaveDraftAsync() 
        │   └─ SaveDepartmentJsonAsync(reportId, dept, JSON, markSubmitted=false)
        ├─ SubmitDepartmentAsync()
        │   └─ SaveDepartmentJsonAsync(reportId, dept, JSON, markSubmitted=true)
        └─ SubmitFullReportAsync() [Only if all depts submitted + CanInitiateReportSubmission]
            └─ SubmitReportToPresidentAsync(reportId)
```

**Key Point**: The editor doesn't care about state machine. It just loads/saves department JSON.

---

## 5. DATA FLOW: Unit President (Middle Level)

```
Dashboard.razor → <UnitPresidentDashboard CurrentUser="@CurrentUser" />
    ├─ Injects: CurrentUserReportService
    ├─ On Load:
    │   ├─ EnsureActiveCycleAsync()
    │   ├─ GetMyUnitReportsAsync(cycleId)
    │   │   └─ Fetches ALL reports submitted to this unit in the cycle
    │   │   └─ Filters: Status is SubmittedToPresident, RejectedByPresident, etc.
    │   └─ Display table with:
    │       ├─ Report ID
    │       ├─ Status (badge)
    │       ├─ Submitted timestamp
    │       ├─ Department count (submitted/total)
    │       ├─ Notes input field
    │       └─ Action buttons (if CanUnitLeadershipAct)
    └─ Actions:
        ├─ Approve(reportId) → ApproveAtUnitAsync()
        │   └─ UnifiedReportService.ApproveByUnitLeadershipAsync()
        │   └─ Status: Draft/SubmittedToPresident/RejectedByPresident → SubmittedToState
        └─ Reject(reportId) → RejectAtUnitAsync()
            └─ UnifiedReportService.RejectByUnitLeadershipAsync()
            └─ Status: SubmittedToPresident → RejectedByPresident
```

**Unit President's View**: Reviews unit officers' submitted reports, forwards to state.

---

## 6. DATA FLOW: State Leadership (Upper-Middle Level)

```
Dashboard.razor → <StateGSDashboard CurrentUser="@CurrentUser" />
    ├─ Injects: CurrentUserReportService
    ├─ On Load:
    │   ├─ EnsureActiveCycleAsync()
    │   ├─ GetMyStateReportsAsync(cycleId)
    │   │   └─ Fetches ALL unit reports in this state for the cycle
    │   ├─ EnsureMyStateReportAsync(cycleId)
    │   │   └─ Gets/creates the STATE-LEVEL aggregated report
    │   ├─ StateReportFormMapper.ToForm(stateReport) ← Parse state form
    │   └─ Display:
    │       ├─ State-level form (attendance, performance, challenges, support needed)
    │       ├─ Aggregated programs (auto-computed from submitted depts)
    │       └─ Review queue of unit reports with action buttons
    └─ Actions:
        ├─ SaveStateDraftAsync()
        │   └─ SaveMyStateReportAsync(stateReportId, form, markSubmitted=false)
        ├─ SubmitStateReportAsync()
        │   └─ SaveMyStateReportAsync(stateReportId, form, markSubmitted=true)
        │   └─ Status: Draft → SubmittedToNational
        ├─ Approve(reportId) → ApproveAtStateAsync()
        │   └─ Status: SubmittedToState → SubmittedToNational
        └─ Reject(reportId) → RejectAtStateAsync()
            └─ Status: SubmittedToState → RejectedByState
```

**State Leadership's View**: Reviews unit reports, creates state-level report, forwards all to national.

**Key Issue #2**: StateGSDashboard reads TWO different report types:
- `List<Report>` via `GetMyStateReportsAsync()` (unit reports)
- `StateReport?` via `GetMyStateReportAsync()` (state aggregated)

→ **Naming is confusing. Recommend:**
  - `GetMyStateReportsAsync()` → `GetStateUnitReportsAsync()` (clearer intent)
  - `GetMyStateReportAsync()` → `GetMyStateAggregateReportAsync()` (singular vs plural naming issue solved)

---

## 7. DATA FLOW: National Leadership (Top Level)

```
Dashboard.razor → <NationalDashboard CurrentUser="@CurrentUser" />
    ├─ Injects: CurrentUserReportService
    ├─ On Load:
    │   ├─ EnsureActiveCycleAsync()
    │   ├─ GetNationalReportsAsync(cycleId)
    │   │   └─ Fetches ALL unit reports nationwide for the cycle
    │   └─ Display table with:
    │       ├─ State name
    │       ├─ Unit name
    │       ├─ Report Status
    │       ├─ Forwarded by state timestamp
    │       ├─ Notes input field
    │       └─ Acknowledge button
    └─ Actions:
        └─ Acknowledge(reportId) → AcknowledgeAtNationalAsync()
            └─ Status: SubmittedToNational → Acknowledged
```

**National Leadership's View**: Reviews all reports nationwide, acknowledges completion.

---

## 8. SERVICE CALL STACK (Backend → Frontend)

### **CurrentUserReportService (Frontend wrapper)**
- Extracts current user context via `AMSAAuthStateProvider.GetCurrentUser()`
- Passes user context to backend methods
- Centralizes authorization checks (throws if not logged in)

```csharp
private Task<T> ExecuteAsCurrentUserAsync<T>(Func<AuthContext, Task<T>> operation) =>
    operation(GetCurrentUserOrThrow());

// Example usage:
public Task<List<Report>> GetMyUnitReportsAsync(int? cycleId = null, CancellationToken ct = default) =>
    ExecuteAsCurrentUserAsync(actor => 
        _reportService.GetUnitReportsAsync(actor, actor.UnitId, cycleId, ct));
```

### **UnifiedReportService (Backend)**
- Contains all business logic
- Takes `AuthContext actor` parameter
- Performs authorization checks via `ReportAccessService`
- Manages database operations

**Example Flow:**
```
DepartmentOfficerDashboard
    ↓ Calls
CurrentUserReportService.GetMyUnitReportsAsync(cycleId)
    ↓ Which calls
ExecuteAsCurrentUserAsync(actor => 
    _reportService.GetUnitReportsAsync(actor, actor.UnitId, cycleId, ct))
    ↓ Which extracts current user and calls
UnifiedReportService.GetUnitReportsAsync(actor, actor.UnitId, cycleId, ct)
    ↓ Which checks
_access.CanReviewAtUnitLevel(actor, unitId)
    ↓ Then queries
_db.Reports.Where(r => r.UnitId == unitId && r.CycleId == cycleId).ToListAsync()
```

---

## 9. KEY DISCREPANCIES & INCONSISTENCIES

### **Issue #1: Role Checking Inconsistency**
**Location**: Dashboard.razor

**Current Code**:
```csharp
if (CurrentUser.GetDashboard() == "NationalDashboard") { ... }
else if (CurrentUser.GetDashboard() == "StateDashboard") { ... }
else if (CurrentUser.IsUnitLeadership) { ... }
else if (CurrentUser.GetDashboard() == "UnitDashboard") { ... }
```

**Problems**:
- Mixes string comparisons (`GetDashboard()`) with boolean properties (`IsUnitLeadership`)
- `GetDashboard()` returns a string—fragile and not type-safe
- Logic order matters: if a user has multiple roles, which dashboard loads?

**Recommendation**:
```csharp
// Suggest: Use enum-based role check
enum DashboardRole { National, State, UnitLeader, DepartmentOfficer }

if (CurrentUser.Role == DashboardRole.National) { ... }
else if (CurrentUser.Role == DashboardRole.State) { ... }
// etc.
```

---

### **Issue #2: Plural/Singular Method Naming (GetMyStateReportAsync vs GetMyStateReportsAsync)**
**Location**: CurrentUserReportService

**Current Code**:
```csharp
// Returns a single StateReport (state-level aggregated view)
public Task<StateReport?> GetMyStateReportAsync(int cycleId, CancellationToken ct = default) =>
    ExecuteAsCurrentUserAsync(actor => 
        _reportService.GetStateReportAsync(actor, actor.StateId, cycleId, ct));

// Returns List<Report> (all UNIT reports in the state)
public Task<List<Report>> GetMyStateReportsAsync(int? cycleId = null, CancellationToken ct = default) =>
    ExecuteAsCurrentUserAsync(actor => 
        _reportService.GetStateReportsAsync(actor, actor.StateId, cycleId, ct));
```

**Problems**:
- Both have similar names but return different types
- `GetMyStateReportsAsync()` actually returns unit reports, not state reports
- StateGSDashboard uses both but the intent is unclear:
  ```csharp
  var reports = await CurrentUserReportService.GetMyStateReportsAsync(cycle.Id);  // Unit reports
  var stateReport = await CurrentUserReportService.GetMyStateReportAsync(cycle.Id); // State aggregate
  ```

**Recommendation**:
```csharp
// Rename for clarity:
public Task<StateReport?> GetMyStateReportAsync(int cycleId, CancellationToken ct = default)
    // No change - singular for single state report

public Task<List<Report>> GetStateUnitReportsAsync(int? cycleId = null, CancellationToken ct = default)
    // Renamed - clarifies these are unit reports, not state reports
```

---

### **Issue #3: Cycle Parameter Inconsistency**
**Location**: CurrentUserReportService and UnifiedReportService

**Current Code**:
```csharp
// Singular: requires cycleId (non-nullable)
public Task<StateReport?> GetMyStateReportAsync(int cycleId, CancellationToken ct = default)

// Plural: cycleId is optional, falls back to active cycle
public Task<List<Report>> GetMyStateReportsAsync(int? cycleId = null, CancellationToken ct = default)
```

**Problem**:
- Inconsistent contract: when should caller pass cycleId vs letting service resolve it?
- No clear pattern for other methods

**Recommendation**:
```csharp
// Make it explicit in method signature
public Task<StateReport?> GetMyStateReportAsync(int? cycleId = null, CancellationToken ct = default)
    // If cycleId is null, resolve to active cycle
    // Matches list pattern

// Or use overloads
public Task<StateReport?> GetMyStateReportAsync(CancellationToken ct = default)
    => GetMyStateReportAsync(null, ct);

public Task<StateReport?> GetMyStateReportAsync(int cycleId, CancellationToken ct = default)
    => ...
```

---

### **Issue #4: State Machine vs Components**
**Location**: DepartmentOfficerDashboard.razor

**Current Code**:
```csharp
private enum DashboardState 
{ 
    NoReportExists, 
    DraftExists, 
    Rejected, 
    SubmittedToPresident, 
    Acknowledged 
}

private DashboardState CurrentState;

// In template:
@switch (CurrentState) {
    case DashboardState.NoReportExists: @RenderNoReportExistsState() break;
    case DashboardState.DraftExists: @RenderDraftExistsState() break;
    // ...
}
```

**Problem**:
- Component duplicates the `Report.Status` logic with a local state machine
- What if Report.Status changes externally? Local state gets out of sync
- Multiple places define workflow logic:
  - `DepartmentOfficerDashboard` (component state)
  - `Report.Status` (entity state)
  - `ReportStatus` enum (backend)

**Recommendation**:
```csharp
// Single source of truth: Report.Status
private Report? CurrentReport;

private DashboardState MapToUIState(Report report) => report.Status switch
{
    ReportStatus.Draft => CurrentReport is null 
        ? DashboardState.NoReportExists 
        : DashboardState.DraftExists,
    ReportStatus.RejectedByPresident or ReportStatus.RejectedByState => DashboardState.Rejected,
    ReportStatus.SubmittedToPresident => DashboardState.SubmittedToPresident,
    ReportStatus.Acknowledged => DashboardState.Acknowledged,
    _ => DashboardState.DraftExists
};

// Use it:
@switch(MapToUIState(CurrentReport)) { ... }
```

---

### **Issue #5: Authorization Checks Scattered**
**Location**: Multiple components and service

**Current Code**:
```csharp
// In Component (DepartmentReportEditor):
if (!CurrentUserReportService.CanEditOwnUnitDepartment(_department))
{
    Nav.NavigateTo("/dashboard", replace: true);
    return;
}

// In Service (CurrentUserReportService):
public bool CanEditOwnUnitDepartment(DepartmentType department)
{
    var user = _authStateProvider.GetCurrentUser();
    var access = new ReportAccessService();
    return access.CanEditDepartment(user, ...);
}

// In Backend (UnifiedReportService):
if (!_access.CanEditDepartment(actor, report.UnitId, report.StateId, department))
    throw new UnauthorizedAccessException(...);
```

**Problem**:
- Authorization checks on frontend (can be bypassed)
- Authorization checks again on backend (correct but creates duplication)
- Creates security gap if frontend check is removed

**Recommendation**:
```csharp
// Frontend should only do optimistic UI hiding:
@if (CanUserEditThisDepartment())
{
    <form> ... </form>
}
else
{
    <div class="alert">You don't have permission</div>
}

// Backend should ALWAYS authorize (cannot be bypassed)
// Current pattern is correct; frontend check is nice-to-have, not security-critical
```

---

### **Issue #6: Navigation & Routing Edge Cases**
**Location**: DepartmentReportEditor.razor

**Current Code**:
```csharp
protected override async Task OnInitializedAsync()
{
    if (!TryMapDepartment(DepartmentSlug, out _department))
    {
        Nav.NavigateTo("/dashboard");
        return;
    }

    var report = await CurrentUserReportService.GetCurrentUserDraftAsync(cycle.Id);
    if (report is null)
    {
        Nav.NavigateTo("/dashboard", replace: true);
        return;
    }

    if (report.Status is not ReportStatus.Draft and not ReportStatus.RejectedByPresident ...)
    {
        Nav.NavigateTo("/dashboard", replace: true);
        return;
    }
}
```

**Problems**:
- Multiple unconditional redirects in init
- User sees blank page while redirect processes
- Loading state shown but immediately redirected
- No error message displayed

**Recommendation**:
```csharp
private bool ShouldRender = false;
private string? RedirectReason;

protected override async Task OnInitializedAsync()
{
    try 
    {
        // ... validation logic
        ShouldRender = true;
    }
    catch (Exception ex)
    {
        RedirectReason = ex.Message;
        await Task.Delay(1000);  // Brief delay to show message
        Nav.NavigateTo("/dashboard");
    }
}

// In template:
@if (!ShouldRender)
{
    @if (!string.IsNullOrEmpty(RedirectReason))
    {
        <div class="alert alert-warning">@RedirectReason</div>
    }
    else
    {
        <div class="alert alert-info">Loading...</div>
    }
}
else
{
    // Render form
}
```

---

## 10. CALL CHAIN SUMMARY

### **From Department Officer**
```
DepartmentOfficerDashboard
  ↓ Load
CurrentUserReportService.EnsureActiveCycleAsync()
  ↓
CurrentUserReportService.GetCurrentUserDraftAsync(cycleId)
  ↓ User clicks "Create Report"
CurrentUserReportService.CreateCurrentUserDraftAsync(cycleId)
  ↓ Navigate to
/report/taleem (or other department)
  ↓ Open
DepartmentReportEditor (cycleId embedded in context)
  ↓ On Load
CurrentUserReportService.GetCurrentUserDraftAsync(cycleId)
CurrentUserReportService.GetDepartmentAsync(reportId, DepartmentType.Taleem)
  ↓ User fills form and clicks "Save Draft"
CurrentUserReportService.SaveDepartmentJsonAsync(reportId, dept, json, false)
  ↓ Save
UnifiedReportService.SaveDepartmentDataAsync(reportId, dept, json, actor, false)
  ↓ DB
INSERT/UPDATE DepartmentReports
  ↓ User submits
CurrentUserReportService.SaveDepartmentJsonAsync(reportId, dept, json, true)
  ↓ Mark
DepartmentReports.IsSubmitted = true, SubmittedAt = now
```

### **From Unit President**
```
Dashboard.razor routes to UnitPresidentDashboard
  ↓
CurrentUserReportService.GetMyUnitReportsAsync(cycleId)
  ↓
UnifiedReportService.GetUnitReportsAsync(actor, unitId, cycleId)
  ↓ Display reports in table
  ↓ User clicks "Approve + Forward"
CurrentUserReportService.ApproveAtUnitAsync(reportId, notes)
  ↓
UnifiedReportService.ApproveByUnitLeadershipAsync(actor, reportId, notes)
  ↓ Update
Report.Status = SubmittedToState
Report.ApprovedByPresidentAt = now
```

### **From State Leadership**
```
Dashboard.razor routes to StateGSDashboard
  ↓ Load unit reports
CurrentUserReportService.GetMyStateReportsAsync(cycleId)
  ↓ Load state aggregate
CurrentUserReportService.EnsureMyStateReportAsync(cycleId)
  ↓ User fills state form
StateReportFormMapper.ToForm(stateReport)
  ↓ User submits
CurrentUserReportService.SaveMyStateReportAsync(stateReportId, form, true)
  ↓
UnifiedReportService.SaveStateReportAsync(actor, stateReportId, form, true)
  ↓ Update
StateReport.Status = SubmittedToNational
  ↓ Also recompute
RecomputeStateAggregateAsync(stateReport)
  ↓ Update unit report actions
CurrentUserReportService.ApproveAtStateAsync(reportId, notes)
  ↓
UnifiedReportService.ApproveByStateLeadershipAsync(actor, reportId, notes)
```

---

## 11. VISUAL WORKFLOW DIAGRAM

```
┌─────────────────────────────────────────────────────────────────┐
│                        AMSA REPORTING WORKFLOW                   │
└─────────────────────────────────────────────────────────────────┘

DEPARTMENT OFFICER LEVEL
────────────────────────
DepartmentOfficerDashboard
    │
    ├─→ Create Draft Report (EnsureDraftAsync)
    │        │
    │        └─→ /report/taleem (DepartmentReportEditor)
    │               │
    │               ├─→ SaveDraft (IsSubmitted = false)
    │               └─→ Submit Department (IsSubmitted = true)
    │
    └─→ Draft appears in UnitPresidentDashboard


UNIT PRESIDENT LEVEL
────────────────────
UnitPresidentDashboard
    │
    ├─→ View unit reports (GetMyUnitReportsAsync)
    │
    ├─→ Review → Approve (ApproveByUnitLeadershipAsync)
    │   └─→ Status: Draft → SubmittedToPresident → SubmittedToState
    │
    └─→ Review → Reject (RejectByUnitLeadershipAsync)
        └─→ Status: SubmittedToPresident → RejectedByPresident
            (Returns to DepartmentOfficer for revision)


STATE LEADERSHIP LEVEL
──────────────────────
StateGSDashboard
    │
    ├─→ View unit reports (GetMyStateReportsAsync)
    │
    ├─→ Create/Edit State Report (EnsureMyStateReportAsync)
    │   ├─→ Fill state-level form
    │   ├─→ Submit state report (SaveMyStateReportAsync)
    │   └─→ Status: Draft → SubmittedToNational
    │
    ├─→ Approve unit reports (ApproveByStateLeadershipAsync)
    │   └─→ Status: SubmittedToState → SubmittedToNational
    │
    └─→ Reject unit reports (RejectByStateLeadershipAsync)
        └─→ Status: SubmittedToState → RejectedByState


NATIONAL LEADERSHIP LEVEL
─────────────────────────
NationalDashboard
    │
    ├─→ View all reports (GetNationalReportsAsync)
    │
    └─→ Acknowledge (AcknowledgeByNationalAsync)
        └─→ Status: SubmittedToNational → Acknowledged
```

---

## 12. RECOMMENDED FIXES (Priority Order)

### **P0 - Security/Correctness**
1. Ensure **all** backend authorization checks are in place (currently correct)
2. Never rely on frontend auth checks alone
3. Validate cycleId on every call (currently done)

### **P1 - Code Quality**
1. **Rename methods** for clarity:
   - `GetMyStateReportsAsync()` → `GetStateUnitReportsAsync()`
   - Update all callers

2. **Standardize role checking**:
   - Replace `GetDashboard()` string comparisons with enum
   - Single source of truth in Dashboard.razor

3. **Standardize cycleId handling**:
   - Make all methods accept `int? cycleId = null`
   - Or make all require explicit cycleId
   - Choose one pattern

### **P2 - UX/Robustness**
1. Show user-friendly errors before redirecting
2. Prevent component from rendering blank pages
3. Use consistent loading states
4. Test all edge cases (expired cycles, no permissions, etc.)

### **P3 - Maintenance**
1. Add XML doc comments to all public methods
2. Document the workflow diagram in codebase
3. Create unit tests for state transitions
4. Add integration tests for full workflows

---

## 13. SERVICE & COMPONENT DEPENDENCY MAP

```
CORE SERVICES (Backend)
├─ UnifiedReportService          (all business logic)
├─ ReportAccessService           (authorization checks)
├─ ReportActivityLogsService     (audit trail)
└─ AmsaDirectoryLookupCache      (lookups)

WRAPPER SERVICES (Frontend)
├─ CurrentUserReportService      (user context wrapper)
├─ AMSAAuthStateProvider         (authentication state)
└─ NavigationManager             (routing)

COMPONENTS (Pages)
├─ Login.razor
├─ Dashboard.razor
│  ├─ DepartmentOfficerDashboard.razor
│  ├─ UnitPresidentDashboard.razor
│  ├─ StateGSDashboard.razor
│  └─ NationalDashboard.razor
├─ DepartmentReportEditor.razor
└─ MainLayout.razor

LAYOUT COMPONENTS
├─ NavMenu.razor
└─ ReconnectModal.razor
```

---

## 14. NEXT STEPS

1. Review this document with the team
2. Decide on naming convention fixes (Issue #2)
3. Decide on role checking pattern (Issue #1)
4. Add XML documentation
5. Create comprehensive test coverage
6. Update component initialization to show error messages
7. Consider state management library if complexity grows further
