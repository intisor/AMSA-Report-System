# State Aggregation vs Leadership Form - Visual Architecture

## Overview Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           MONTHLY REPORTING CYCLE                           │
└─────────────────────────────────────────────────────────────────────────────┘

                    Unit Level (5 departments submitted)
                                    │
                                    ▼
┌──────────────────────────────────────────────────────────────────────────┐
│ UNIT 42 - REPORT ROW                                                     │
│ ├─ Report.Id = 9001                                                      │
│ ├─ Report.Status = SubmittedToState                                      │
│ └─ DepartmentReports (5 rows - one per dept, all IsSubmitted=true)       │
│    ├─ Taleem: {"sessions":5}, AttendanceCount=120                        │
│    ├─ Welfare: {"programs":2}, BeneficiaryCount=30                       │
│    ├─ Health: AttendanceCount=45, BeneficiaryCount=10                    │
│    ├─ Finance: DuesCollected=250.00                                      │
│    └─ Sport: AttendanceCount=60                                          │
└──────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ (other units also submit...)
                                    ▼
┌──────────────────────────────────────────────────────────────────────────┐
│ STATE LEADERSHIP INITIATES STATE REPORT                                  │
│ ├─ Creates or opens StateReport (StateId=7, CycleId=202406)              │
│ └─ Calls RecomputeStateAggregateAsync()                                  │
└──────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    ▼                               ▼
        ┌─────────────────────┐       ┌──────────────────────┐
        │ AUTO-AGGREGATION    │       │ LEADERSHIP FORM      │
        │ (Derived Metrics)   │       │ (Manual Edits)       │
        └─────────────────────┘       └──────────────────────┘
                    │                               │
                    ▼                               ▼
```

---

## The Two Parallel Paths

### PATH 1: AUTO-AGGREGATION (Automatic, Refreshable)

```
RecomputeStateAggregateAsync() runs:

1. QUERY
   ├─ Get all Report rows for StateId=7, CycleId=202406
   └─ Get all submitted DepartmentReport rows across those reports

2. COMPUTE METRICS
   ├─ UnitsAttendedTo = count of units with Status >= SubmittedToState
   ├─ TotalUnitReportsCount = count of all unit reports
   └─ UnitPerformanceRating = (UnitsAttendedTo / TotalUnitReportsCount) * 100

3. AGGREGATE PROGRAMS (by Department)
   ├─ Taleem: TotalAttendance = SUM(all unit Taleem AttendanceCount)
   │          TotalBeneficiaries = SUM(all unit Taleem BeneficiaryCount)
   │          IsAutoAggregated = TRUE  ◄─── KEY FLAG
   │
   ├─ Welfare: TotalAttendance=..., TotalBeneficiaries=30, IsAutoAggregated=TRUE
   ├─ Health: TotalAttendance=45, TotalBeneficiaries=10, IsAutoAggregated=TRUE
   └─ ...etc

4. PRESERVE MANUAL EDITS
   ├─ Find existing StateReportProgram rows where IsAutoAggregated=FALSE (manual)
   ├─ Keep those rows untouched ◄─── SAFE!
   └─ Remove only rows where IsAutoAggregated=TRUE (old auto rows)

5. INSERT NEW AGGREGATES
   └─ Add fresh StateReportProgram rows with IsAutoAggregated=TRUE

6. MARK AGGREGATION METADATA
   ├─ StateReport.IsAggregated = true
   ├─ StateReport.LastAggregatedAt = NOW()
   └─ StateReport.LastAggregatedByMemberId = actor.MemberId (optional)

7. PERSIST
   └─ SaveChangesAsync()

RESULT: StateReport.Programs contains both auto and manual rows, clearly marked.
```

---

### PATH 2: LEADERSHIP FORM (Manual Edits by State Leadership)

```
SaveStateReportAsync(form) runs:

1. ACCEPT FORM INPUT (Leadership Commentary)
   ├─ form.UnitImprovementPlan = "..."
   ├─ form.ChallengesFaced = "..."
   ├─ form.NationalSupportNeeded = "..."
   └─ form.OtherNotes = "..."

2. UPDATE FORM FIELDS (NOT aggregated metrics)
   ├─ stateReport.UnitImprovementPlan = form.UnitImprovementPlan
   ├─ stateReport.ChallengesFaced = form.ChallengesFaced
   ├─ stateReport.NationalSupportNeeded = form.NationalSupportNeeded
   └─ stateReport.OtherNotes = form.OtherNotes

3. CALL AGGREGATION
   └─ await RecomputeStateAggregateAsync(stateReport, ct)
      (This refreshes metrics without clobbering the form fields above!)

4. OPTIONALLY MARK AS SUBMITTED
   ├─ if (markSubmitted)
   │  ├─ stateReport.Status = SubmittedToNational
   │  ├─ stateReport.SubmittedAt = NOW()
   │  └─ stateReport.SubmittedByMemberId = actor.MemberId
   └─ endif

5. PERSIST
   └─ SaveChangesAsync()

RESULT: Leadership fields preserved, metrics refreshed, aggregation date recorded.
```

---

## State Report Entity - What Gets Stored Where

```
StateReport Row (One per State + Cycle)
│
├─ LEADERSHIP FORM FIELDS (manual edits, persist across aggregations)
│  ├─ UnitImprovementPlan ──► State leader writes improvement plan
│  ├─ ChallengesFaced ──────► State leader documents challenges
│  ├─ NationalSupportNeeded ► State leader flags escalation needs
│  └─ OtherNotes ──────────► State leader adds comments
│
├─ AUTO-AGGREGATED METRICS (computed, refreshable)
│  ├─ UnitsAttendedTo ───────► Derived from Report.Status counts
│  ├─ TotalUnitReportsCount ─► Derived from all unit reports
│  └─ UnitPerformanceRating ─► Derived: (UnitsAttendedTo/Total)*100
│
├─ AGGREGATION METADATA (marks when and who ran aggregation)
│  ├─ IsAggregated ──────────► TRUE after first aggregation
│  ├─ LastAggregatedAt ──────► DATETIME when Recompute() last ran
│  └─ LastAggregatedByMemberId ► INT member ID of last aggregation runner
│
├─ SUBMISSION FIELDS
│  ├─ Status ───────────────► Draft, SubmittedToNational, etc.
│  ├─ SubmittedAt ──────────► DATETIME when submitted
│  └─ SubmittedByMemberId ──► INT member ID who submitted
│
└─ NAVIGATIONS
   └─ Programs (ICollection<StateReportProgram>)
      ├─ Row with IsAutoAggregated=TRUE  ◄─── Auto-computed
      ├─ Row with IsAutoAggregated=FALSE ◄─── Manually created
      └─ ... (one row per program/department, mixed sources)
```

---

## Example Data Snapshot (After First Aggregation)

```
StateReports Table:
┌────┬───────┬────────┬────────────────┬───────────────────┬──────────────────────┐
│ Id │StateId│CycleId │ UnitImprovment │ IsAggregated      │LastAggregatedAt      │
├────┼───────┼────────┼────────────────┼───────────────────┼──────────────────────┤
│1200│   7   │ 202406 │ "Train 3 more" │ TRUE              │2025-05-13T14:30:00Z  │
└────┴───────┴────────┴────────────────┴───────────────────┴──────────────────────┘

StateReportPrograms Table (for StateReportId=1200):
┌──────┬────────────────────┬──────────────────────┬──────────────────┬─────────────────┐
│ PId  │ ProgramName        │ TotalAttendance      │ TotalBeneficiaries│IsAutoAggregated │
├──────┼────────────────────┼──────────────────────┼──────────────────┼─────────────────┤
│ 5001 │ Taleem             │ 120                  │ 0                 │ TRUE (auto)     │
│ 5002 │ Welfare            │ 0                    │ 30                │ TRUE (auto)     │
│ 5003 │ Health             │ 45                   │ 10                │ TRUE (auto)     │
│ 5004 │ Manual Program XYZ │ 50                   │ 25                │ FALSE (manual)  │ ◄─ NOT TOUCHED
│ 5005 │ Finance            │ 0                    │ 0                 │ TRUE (auto)     │
│ 5006 │ Sport              │ 60                   │ 0                 │ TRUE (auto)     │
└──────┴────────────────────┴──────────────────────┴──────────────────┴─────────────────┘

When Recompute runs again:
✓ Rows 5001-5003, 5005-5006 (IsAutoAggregated=TRUE) are REMOVED and re-created
✗ Row 5004 (IsAutoAggregated=FALSE) is PRESERVED - state leader's manual edit survives!
```

---

## Workflow Sequence (Mermaid-style Timeline)

```
Time    Event                                    StateReport State
────────────────────────────────────────────────────────────────────────────────
T0      Unit reports submitted                   IsAggregated=FALSE
        ↓
T1      State leadership creates StateReport     (auto)
        ↓
T2      RecomputeStateAggregateAsync() runs     IsAggregated=TRUE
        │                                        LastAggregatedAt=T2
        │ ✓ UnitsAttendedTo computed
        │ ✓ StateReportPrograms populated (all auto)
        ↓
T3      State leader edits form & adds manual    UnitImprovementPlan updated
        program (IsAutoAggregated=FALSE)         SubmittedAt=NULL (still draft)
        ↓
T4      Recompute runs again (e.g., more units  IsAggregated=TRUE
        submitted)                              LastAggregatedAt=T4
        │                                        Programs: 6 auto rows + 1 manual row
        │ ✓ Auto rows refreshed                 (manual preserved!)
        │ ✓ Manual row untouched
        ↓
T5      State leader submits to national        Status=SubmittedToNational
        (form fields + programs locked)         SubmittedAt=T5
```

---

## When to Use Each Path

### Use AUTO-AGGREGATION (RecomputeStateAggregateAsync)
- **When:** Units submit/update their department reports
- **Why:** Metrics need to stay current and reflect latest unit submissions
- **Effect:** StateReportProgram rows with IsAutoAggregated=TRUE are refreshed; manual rows survive
- **Call Context:** Triggered by SaveStateReportAsync or GetStateReportAsync

### Use LEADERSHIP FORM (SaveStateReportAsync)
- **When:** State leadership enters narrative, plans, or escalation needs
- **Why:** Leadership insight and oversight cannot be automated
- **Effect:** Form fields updated, then Recompute runs to refresh metrics without losing form data
- **Call Context:** Blazor form submission from StateLeadershipDashboard

---

## Key Safety Guarantee

```
BEFORE Aggregation                AFTER Aggregation
├─ Auto rows (5) ──► REPLACED    ├─ Auto rows (5) ──► NEW (refreshed)
├─ Manual row (1) ─► PRESERVED   ├─ Manual row (1) ─► SAME ✓
└─ Form fields ────► STABLE      └─ Form fields ────► STABLE ✓

OLD RISK: RecomputeStateAggregateAsync() → RemoveRange(ALL programs) 
          → State leader's manual row LOST!

NEW SAFE: RemoveRange(programs WHERE IsAutoAggregated=TRUE)
          → Manual rows survive ✓
          → Aggregation can run freely ✓
          → Leadership edits never clobbered ✓
```

---

## Database Migration Required

```sql
ALTER TABLE StateReports
ADD IsAggregated BIT NOT NULL DEFAULT 0,
    LastAggregatedAt DATETIME NULL,
    LastAggregatedByMemberId INT NULL;

ALTER TABLE StateReportPrograms
ADD IsAutoAggregated BIT NOT NULL DEFAULT 1;
```

**Applied via:**
```powershell
dotnet ef database update -p AMSAReportingSystem
```

---

## Next Steps (Optional Enhancements)

```
1. UI: Add "Refresh Aggregation" button to StateLeadershipDashboard
   ├─ Shows LastAggregatedAt timestamp
   └─ Allows manual re-run of Recompute

2. Audit: Set LastAggregatedByMemberId when aggregation runs
   └─ Requires passing actor context through Recompute method

3. Workflow: Lock form edits after submission
   ├─ Status=SubmittedToNational → read-only UI
   └─ Prevent accidental changes during national review

4. Reporting: Show which programs are auto vs manual in UI
   ├─ Filter by IsAutoAggregated in views
   └─ Help state leaders understand data provenance
```

---

## Summary

| Aspect | AUTO-AGGREGATION | LEADERSHIP FORM |
|--------|---|---|
| **Data** | Metrics (attendance, beneficiaries, counts) | Commentary (plans, challenges, notes) |
| **Source** | Computed from unit DepartmentReports | Human entry by state leadership |
| **Refresh** | Yes (runs repeatedly, safe) | Stable (persists across aggregations) |
| **Marked By** | `IsAutoAggregated = TRUE` | `IsAutoAggregated = FALSE` |
| **Risk** | Without flags → overwrites manual edits | Without tracking → can't distinguish origin |
| **Protection** | Only delete/replace rows where `IsAutoAggregated=TRUE` | Flag preserved, can check on load |

This separation ensures **leadership commentary never conflicts with metric refresh cycles**.
