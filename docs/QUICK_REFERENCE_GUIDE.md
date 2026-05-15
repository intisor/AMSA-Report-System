# AMSA Reporting System - Quick Reference Guide

## 📋 How Components & Services Work Together

### **The Big Picture**
Your application follows a **hierarchical approval chain**: 
**Department Officers** → **Unit Presidents** → **State Leadership** → **National Leadership**

Each role has a dashboard that shows their "queue" of reports to review.

---

## 🎯 Component Map (What calls what)

```
Frontend                               Backend                    Database
─────────────────────────────────────────────────────────────────────────────

Dashboard.razor                        (no direct calls)
    ↓ Routes to role-based
    ├─→ DepartmentOfficerDashboard  ──→ CurrentUserReportService ──→ UnifiedReportService ──→ DbContext ──→ SQL
    ├─→ UnitPresidentDashboard      ──┐
    ├─→ StateGSDashboard            ──┤
    └─→ NationalDashboard           ──┴→ (all use same pattern)

DepartmentReportEditor.razor           (specific form editing)
    └─→ CurrentUserReportService       ──→ Same as above
```

---

## 🔄 The Call Pattern

**Every** user-initiated action follows this pattern:

```csharp
// 1. Component calls frontend service (with user context implicit)
await CurrentUserReportService.SubmitReportToPresidentAsync(reportId);

// 2. Frontend service extracts current user
private Task<T> ExecuteAsCurrentUserAsync<T>(Func<AuthContext, Task<T>> operation) =>
    operation(GetCurrentUserOrThrow());  // Gets user from AMSAAuthStateProvider

// 3. Frontend service calls backend with user context
public Task<Report> SubmitReportToPresidentAsync(int reportId, string? notes = null, ...) =>
    ExecuteAsCurrentUserAsync(actor =>                      // ← actor = current user
        _reportService.SubmitReportToPresidentAsync(actor, reportId, notes, ct));

// 4. Backend performs auth check
public async Task<Report> SubmitReportToPresidentAsync(AuthContext actor, int reportId, ...)
{
    if (!_access.CanInitiateReportSubmission(actor, report.UnitId, report.StateId))
        throw new UnauthorizedAccessException("Not allowed!");  // ← Fails if unauthorized

    // ... business logic ...

    await _db.SaveChangesAsync(ct);  // ← Hits database
}
```

**Key insight**: **Frontend wrapper automatically passes user context.** Backend always checks authorization.

---

## 📍 Each Dashboard's Role

### **1. DepartmentOfficerDashboard**
- **What it shows**: Current user's report(s) for the active cycle
- **What it does**: 
  - Creates new report (all 11 departments initialized as Draft)
  - Navigates to department editors (e.g., `/report/taleem`)
  - Shows report status (Draft → SubmittedToPresident → Acknowledged)
- **Data source**: `GetCurrentUserDraftAsync()` (single report)
- **State machine**: Maps `Report.Status` to UI states (Draft, Rejected, Submitted, Acknowledged)

### **2. UnitPresidentDashboard**
- **What it shows**: All reports submitted by department officers in the unit
- **What it does**:
  - Reviews each report (departments submitted, status)
  - Fills optional note
  - Approves → forwards to state (Status: SubmittedToState)
  - Rejects → sends back to department officer (Status: RejectedByPresident)
- **Data source**: `GetMyUnitReportsAsync()` (list of reports)
- **Approval action**: ApproveByUnitLeadershipAsync()

### **3. StateGSDashboard**
- **What it shows**:
  - **State report form** (one per state per cycle) with auto-aggregated data
  - **Unit reports list** (all reports in the state awaiting state review)
- **What it does**:
  - Fills state-level form (attendance, performance, challenges, programs)
  - Saves as draft or submits to national
  - Reviews unit reports → Approves (→ SubmittedToNational) or Rejects (→ RejectedByState)
- **Data sources**: 
  - `GetMyStateReportsAsync()` (unit reports list)
  - `EnsureMyStateReportAsync()` (state aggregate report)
- **Approval actions**: ApproveByStateLeadershipAsync(), RejectByStateLeadershipAsync()

### **4. NationalDashboard**
- **What it shows**: All reports nationwide in SubmittedToNational status
- **What it does**:
  - Fills optional note
  - Acknowledges completion (Status: Acknowledged) - end of chain
- **Data source**: `GetNationalReportsAsync()` (all reports nationwide)
- **Approval action**: AcknowledgeByNationalAsync()

---

## 🔐 Where Authorization Actually Happens

**Frontend**:
- Checks permissions to show/hide UI elements
- ✅ Nice to have, but not security-critical
- ❌ Can be bypassed (user can edit HTML)

**Backend** (ALWAYS):
- Checks permissions before any database operation
- ✅ Security-critical - cannot be bypassed
- ✅ Throws UnauthorizedAccessException if violated

**Example**: If a malicious user manually edits their browser to call `ApproveByUnitAsync()`, the backend will:
1. Extract user context
2. Check: `_access.CanReviewAtUnitLevel(actor, unitId)`
3. Throw exception and return 403 Forbidden
4. User cannot approve anything

---

## 📊 Key Data Models

### **Report** (unit-level report)
```
Id              : int
UnitId          : int
StateId         : int
CycleId         : int
Status          : ReportStatus (Draft, SubmittedToPresident, SubmittedToState, SubmittedToNational, Acknowledged, etc.)
DepartmentReports : List<DepartmentReport> (one per department type)
CreatedAt       : DateTime
SubmittedToPresidentAt : DateTime?
ApprovedByPresidentAt : DateTime?
ApprovedByStateAt : DateTime?
AcknowledgedByNationalAt : DateTime?
ActivityLogs    : List<ReportActivityLog>
```

### **DepartmentReport** (department-specific data)
```
Id              : int
ReportId        : int
Department      : DepartmentType (Taleem, Tabligh, Welfare, Sport, etc.)
ReportData      : string (JSON - contains form answers)
IsSubmitted     : bool
SubmittedAt     : DateTime?
SubmittedByMemberId : int?
CreatedAt       : DateTime
UpdatedAt       : DateTime
```

### **StateReport** (state-level aggregated report)
```
Id              : int
StateId         : int
CycleId         : int
Status          : ReportStatus
UnitPresidentsAttended : int
TotalUnitPresidents : int
UnitPerformanceRating : int
UnitImprovementPlan : string
ChallengesFaced : string
NationalSupportNeeded : string
OtherNotes      : string
Programs        : List<StateReportProgram> (auto-aggregated from depts)
CreatedAt       : DateTime
SubmittedAt     : DateTime?
```

---

## ⚠️ Known Inconsistencies (Issues)

### **Issue #1: Role Checking Inconsistency**
```csharp
// Dashboard.razor mixes two patterns:
if (CurrentUser.GetDashboard() == "NationalDashboard") { ... }  // string comparison
else if (CurrentUser.IsUnitLeadership) { ... }                  // boolean property
```
**Problem**: Fragile, not type-safe, hard to maintain
**Fix**: Use a single `Role` enum

---

### **Issue #2: Confusing Plural/Singular Names**
```csharp
// These have similar names but return DIFFERENT types!
Task<StateReport?> GetMyStateReportAsync(int cycleId)        // Single state report
Task<List<Report>> GetMyStateReportsAsync(int? cycleId)      // List of UNIT reports
```
**Problem**: StateGSDashboard uses both - easy to mix up
**Fix**: Rename to `GetStateUnitReportsAsync()` for clarity

---

### **Issue #3: Cycle ID Handling**
```csharp
GetMyStateReportAsync(int cycleId)           // Required (non-nullable)
GetMyStateReportsAsync(int? cycleId = null)  // Optional (falls back to active)
```
**Problem**: Inconsistent - when to pass cycleId?
**Fix**: Make all methods accept `int? cycleId = null` for consistency

---

### **Issue #4: State Machine Duplication**
```csharp
// DepartmentOfficerDashboard has its own state machine:
private enum DashboardState { NoReportExists, DraftExists, Rejected, ... }

// But Report entity already has the state:
public ReportStatus Status { get; set; }
```
**Problem**: Two sources of truth - can get out of sync
**Fix**: Map Report.Status directly to UI without intermediate enum

---

### **Issue #5: Premature Navigation (Poor UX)**
```csharp
protected override async Task OnInitializedAsync()
{
    if (!TryMapDepartment(...)) Nav.NavigateTo("/dashboard");  // Blank page → redirect
    if (report is null) Nav.NavigateTo("/dashboard");          // No error message shown
    if (report.Status...) Nav.NavigateTo("/dashboard");        // Silent redirect
}
```
**Problem**: User sees blank page while redirecting, no error message
**Fix**: Show error message briefly before redirecting

---

## 🚀 How to Add a New Feature

### Example: New Dashboard for Finance Review

1. **Create new component**: `FinanceDashboard.razor`
   - Inject: `CurrentUserReportService`
   - Call: `GetUnitReportsAsync()` to list unit reports
   - Show: Finance-specific metrics
   - On action: Call `ApproveAtUnitAsync()` or similar

2. **Add routing in Dashboard.razor**:
   ```csharp
   else if (CurrentUser.Role == DashboardRole.Finance)
   {
       <FinanceDashboard CurrentUser="@CurrentUser" />
   }
   ```

3. **Backend authorization** (already done in UnifiedReportService):
   - Add check: `_access.CanReviewFinance(actor)`
   - Throws if unauthorized

4. **Database** (optional):
   - Add new entity if storing finance-specific data
   - Otherwise use existing DepartmentReport.ReportData (JSON)

---

## 📚 File Structure

```
Components/
├── App.razor                           (root)
├── Routes.razor                        (auto-routing)
├── Pages/
│   ├── Login.razor                    (@page "/login")
│   ├── Dashboard.razor                (@page "/dashboard" - role router)
│   ├── DepartmentReportEditor.razor   (@page "/report/{slug}")
│   ├── Home.razor                     (@page "/")
│   ├── Error.razor                    (error handling)
│   └── NotFound.razor                 (404)
├── [dashboards not marked @page - rendered by Dashboard.razor]
│   ├── DepartmentOfficerDashboard.razor
│   ├── UnitPresidentDashboard.razor
│   ├── StateGSDashboard.razor
│   └── NationalDashboard.razor
├── Layout/
│   ├── MainLayout.razor               (wraps all pages)
│   ├── NavMenu.razor
│   └── ReconnectModal.razor
└── [other components]

Services/
├── CurrentUserReportService.cs        (frontend wrapper)
├── UnifiedReportService.cs            (backend business logic)
├── ReportAccessService.cs             (authorization)
├── AMSAAuthStateProvider.cs           (auth state)
└── [other services]

Data/
└── AMSAReportingDbContext.cs          (Entity Framework)
    └── DbSet<Report>, DbSet<DepartmentReport>, DbSet<StateReport>, ...
```

---

## 🔍 Debugging Tips

**"My component shows blank page"**
- Check: Is `OnInitializedAsync()` redirecting without error message?
- Check: Is user authenticated? (Check browser F12 → Application → Cookies)
- Check: Does user have permission for this page?

**"Backend throws UnauthorizedAccessException"**
- Check: `_access.CanReviewAtUnitLevel(actor, unitId)` - is actor.UnitId correct?
- Check: Is actor.StateId matching report.StateId?
- Check: Is actor's role actually set in database?

**"Data not updating"**
- Check: Is `await _db.SaveChangesAsync(ct)` being called?
- Check: Are you using `.FirstOrDefaultAsync()` or caching?
- Check: Is user hitting cache instead of database?

**"State machine not transitioning"**
- Check: Is Report.Status being updated in backend?
- Check: Is component loading fresh data or using old cached version?
- Check: Does Report.Status match the UI state enum mapping?

---

## 📞 Quick Links

- **Main entry**: App.razor
- **Router**: Routes.razor, Dashboard.razor
- **Services**: CurrentUserReportService, UnifiedReportService
- **Auth**: AMSAAuthStateProvider, ReportAccessService
- **Database**: AMSAReportingDbContext
- **Forms**: DepartmentReportEditor, StateGSDashboard

---

## ✅ Testing Checklist

- [ ] Can department officer create report?
- [ ] Can department officer submit individual departments?
- [ ] Can unit president approve and forward?
- [ ] Can state leadership edit state form?
- [ ] Can state leadership approve unit reports?
- [ ] Can national leadership acknowledge?
- [ ] Are all authorization checks enforced?
- [ ] Does cycle deadline lock submissions?
- [ ] Can rejected reports be revised?
- [ ] Do activity logs record all actions?

---

## 🎓 Key Takeaways

1. **Authorization**: Always check on backend, frontend is UI sugar
2. **State machine**: Report.Status is the single source of truth
3. **Hierarchy**: 4 levels (Dept → Unit → State → National)
4. **Service pattern**: Frontend wrapper extracts user, passes to backend
5. **Database**: Every action creates ReportActivityLog entry (audit trail)
6. **Naming**: Inconsistencies exist - be careful with similar method names
7. **Testing**: Test each role's dashboard and approval actions
