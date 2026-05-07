# Report Lifecycle & Dashboard Pattern

## Problem Statement
- Dashboard currently auto-creates draft reports on every page load, coupling UI rendering to state mutation
- Hardcoded cycle/deadline behavior (465-day cycles, implicit deadline calculations)
- Users see "CycleLabel" without context about whether a report exists
- No explicit user action required to create a draft—happens implicitly during summary load
- This violates the principle: **reports are created only when a user explicitly clicks a create button**

---

## Desired State Flow

### 1. **Dashboard View States**

#### State: `NoActiveCycle`
- **Trigger**: No active reporting cycle found
- **Display**: "No active reporting cycle. Please contact your state administrator."
- **Action**: None available; await cycle creation

#### State: `NoCycle` (Specific cycle required but missing)
- **Trigger**: Department officer navigates to report but cycle is locked/missing
- **Display**: "The reporting cycle has ended. Next cycle: [date]"
- **Action**: None

#### State: `NoReportExists` (Realistic dashboard state)
- **Trigger**: Active cycle exists, but no draft report has been created by the user yet
- **Display**: 
  - Cycle info: "Current Cycle: [CycleMonth]" (e.g., "January 2025")
  - Deadline: "[DeadlineLabel]" (e.g., "January 31, 2025")
  - Status badge: "Not Started"
  - Progress: 0%
  - **Call-to-action button**: "Create Report"
- **Action**: User clicks "Create Report" → triggers `EnsureDraftAsync()`

#### State: `DraftExists` (Report is in progress)
- **Trigger**: Draft report exists for current cycle and user
- **Display**:
  - Cycle info: "[CycleMonth]"
  - Deadline: "[DeadlineLabel]" with days-left counter
  - Status badge: "In Progress"
  - Progress bar: X of Y departments submitted
  - Recent activity feed (last 6 actions)
  - Department quick-access buttons (edit/view each)
  - **Primary action button**: "Continue Report" → navigate to first incomplete dept
  - **Secondary action button**: "Submit Report" (enabled only if all depts submitted)
- **Action**: User edits departments or submits

#### State: `SubmittedToPresident`
- **Trigger**: Draft report marked as submitted to unit president
- **Display**:
  - Cycle info
  - Status badge: "Awaiting Unit Leadership Review"
  - Submission timestamp: "Submitted on [date] at [time]"
  - Read-only department summary
  - **Action**: None (wait for approval/rejection from president)

#### State: `RejectedByPresident` / `RejectedByState`
- **Trigger**: Report rejected at any level with notes
- **Display**:
  - Status badge: "Rejected" (with rejection level)
  - Rejection notes from reviewer
  - **Call-to-action button**: "Revise and Resubmit"
- **Action**: User clicks revise → re-opens draft for editing

#### State: `SubmittedToNational` / `Acknowledged`
- **Trigger**: Report fully approved and acknowledged
- **Display**:
  - Status badge: "Acknowledged"
  - Timeline of approvals (unit president → state → national)
  - Read-only view of all departments
  - **Action**: None

---

## Service Method Contract Changes

### **UnifiedReportService Changes**

#### Current Problematic Flow
```csharp
// Dashboard calls:
CurrentReport = await reportService.EnsureDraftAsync(actor, unitId, cycleId);
// This auto-creates if missing—dashboard cannot distinguish "no report" from "report exists"
```

#### Proposed New Flow
```csharp
// Query methods (no side effects):
Report? GetDraftAsync(actor, unitId, cycleId)  // Returns null if no draft
ReportingCycle? GetActiveCycleAsync()           // Returns active cycle or null
bool CanCreateReportForCycle(actor, cycle)      // Validates eligibility

// Creation method (explicit, called only from create button):
Report CreateDraftAsync(actor, unitId, cycleId) // Creates draft or throws
```

#### Implementation Note
- `GetDraftAsync`: Query only, no `Ensure` semantics
- `CreateDraftAsync`: Named to signal mutation; throws if preconditions fail
- Dashboard calls query methods during load; only calls `CreateDraftAsync` when user clicks button

---

## Dashboard Component Behavior

### **Current (Problematic)**
```
OnParametersSetAsync()
  → LoadSummaryAsync()
    → EnsureActiveCycleAsync()       [auto-creates cycle if missing]
    → EnsureCurrentUserDraftAsync()  [auto-creates draft if missing]
    → Display summary (always shows "in progress" state)
```

### **Proposed (Explicit Create Flow)**
```
OnParametersSetAsync()
  → LoadSummaryAsync()
    → GetActiveCycleAsync()        [no mutation]
    → If no cycle: Display "NoActiveCycle" state
    → Else: GetDraftAsync()        [no mutation]
      → If no draft: Display "NoReportExists" state + create button
      → Else: Display "DraftExists" state + summary

[User Action] Clicks "Create Report" button
  → OnCreateReportAsync()
    → CreateDraftAsync()
    → Refresh summary (now shows DraftExists state)
```

---

## Component Properties & Display

### **Cycle Label** (Remove hardcoding)
- **Before**: `CycleLabel = cycle.CycleMonth` (e.g., "December 2024")
- **After**: Same, but sourced from database cycle record (no implicit generation)
- **UI Display**: Show actual cycle name; if cycle is missing, show informational message

### **Deadline Label** (Make realistic)
- **Before**: `DeadlineLabel = cycle.SubmissionDeadline.ToString("MMMM d, yyyy")`
- **After**: Same, but deadline is explicit in cycle table (not `end.AddDays(DefaultSubmissionGraceDays)`)
- **UI Display**: Show deadline + days remaining dynamically
- **Example**: "Due by January 31, 2025 (15 days remaining)"

### **Days Left Counter** (Dynamic, correct)
- **Before**: Computed once at load; could be stale
- **After**: Recomputed at load and optionally re-rendered periodically (e.g., via SignalR heartbeat or JS timer)

### **Progress & Activity Feed** (Conditional)
- **Only shown in DraftExists state**
- If no draft exists, these are empty/hidden
- Prevents confusion about which cycle/report is active

---

## Reporting Cycle Management (Deterministic, Monthly)

### **Current Issue**
- `EnsureActiveCycleAsync()` auto-creates cycles with hardcoded 7-day grace period
- Not tied to calendar months; creates unrealistic multi-month cycles

### **Proposed (Deterministic, No Admin Manual Work)**
- **One cycle per calendar month, deterministically**
- Cycle start: 1st of current month
- Cycle end: Last day of current month
- Submission deadline: Last day of current month (no grace days)
- `EnsureActiveCycleAsync()` auto-creates cycle for current month if it doesn't exist
  - Safe to call repeatedly; idempotent
  - No hardcoded grace periods
- Dashboard calls `EnsureActiveCycleAsync()` on load to ensure cycle exists; always returns a cycle

### **Implementation Path**
1. Update `EnsureActiveCycleAsync()`:
   - Determine current month's start and end dates
   - Check if cycle for this month exists; return if yes
   - Create cycle with `CycleMonth = "January 2025"`, `StartDate = Jan 1`, `EndDate = Jan 31`, `SubmissionDeadline = Jan 31`
   - No grace days, no implicit locking logic
2. Keep `GetActiveCycleAsync()` query-only; used when cycle must exist
3. Dashboard: Call `EnsureActiveCycleAsync()` to guarantee current month's cycle exists

---

## Error Handling & Resilience

### **Unit Sync Failure Handling** (Already patched)
- If AMSA API unit lookup fails during draft creation:
  1. Try local lookup
  2. Try AMSA API fetch
  3. **Fall back** to creating unit from AuthContext (state + unit from user data)
  4. Log warning with user context
  5. Proceed with draft creation

### **Dashboard Error Handling** (Already improved)
- `LoadSummaryAsync()` wraps all operations in try/catch
- On error: Log message, keep default state on dashboard
- Display user-friendly error message (if needed) instead of throwing

---

## Summary: State Machine Table

| State | Condition | Display | Primary Action | Secondary Action |
|-------|-----------|---------|-----------------|------------------|
| **NoActiveCycle** | `GetActiveCycleAsync()` returns null | "No active cycle" message | None | Refresh (admin-initiated) |
| **NoReportExists** | Cycle exists, `GetDraftAsync()` returns null | Cycle info + "Create Report" button | Create Report | None |
| **DraftExists** | Draft report exists, `Status == Draft` | Summary, progress, activity | Continue/Edit | Submit (if all depts submitted) |
| **SubmittedToPresident** | `Status == SubmittedToPresident` | Status badge, submission date | None (read-only) | None |
| **Rejected** | `Status == RejectedByPresident/State` | Rejection notes + "Revise & Resubmit" | Revise & Resubmit | None |
| **Acknowledged** | `Status == Acknowledged` | Timeline, approvals | None (read-only) | None |

---

## Acceptance Criteria

✅ Dashboard **does not** auto-create draft reports on load  
✅ Dashboard shows **realistic state** based on actual report existence  
✅ User must **explicitly click "Create Report"** to initiate draft  
✅ **No hardcoded** cycle/deadline values; all come from database  
✅ **Cycle label** and **deadline label** are data-driven and realistic  
✅ **Unit sync failure** gracefully falls back to AuthContext  
✅ **Error handling** logs and displays user-friendly messages  
✅ **Admin controls** cycle creation; department officers only view/create reports  

