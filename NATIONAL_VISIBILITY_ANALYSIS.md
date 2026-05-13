# National Leadership View - What Gets Sent Up the Chain

## Current Data Flow: Unit → State → National

```
┌──────────────────────────────────────────────────────────────────┐
│ UNIT LEVEL                                                       │
│ ├─ Department officers fill Report with DepartmentReport JSON   │
│ └─ Unit president approves → Status=SubmittedToState            │
└──────────────────────────────────────────────────────────────────┘
                             │
                             ▼
┌──────────────────────────────────────────────────────────────────┐
│ STATE LEVEL                                                      │
│ ├─ StateReport created (separate entity)                        │
│ ├─ Leadership form (UnitImprovementPlan, Challenges, etc.)      │
│ ├─ Auto-aggregated metrics (UnitsAttendedTo, Programs)          │
│ ├─ State leadership approves → Status=SubmittedToNational      │
│ └─ **StateReport is NOT automatically sent to National**        │
└──────────────────────────────────────────────────────────────────┘
                             │
                             ▼
┌──────────────────────────────────────────────────────────────────┐
│ NATIONAL LEVEL                                                   │
│ GetNationalReportsAsync() returns:                              │
│ ├─ Report rows (Unit-level master)                             │
│ │  ├─ Report.Status = SubmittedToNational                      │
│ │  ├─ Report.DepartmentReports (the 5 departments with JSON)   │
│ │  ├─ Report.PresidentialNotes (from unit president)           │
│ │  ├─ Report.StateNotes (from state leadership)                │
│ │  └─ Report.ActivityLogs (full timeline)                      │
│ │                                                               │
│ └─ **DOES NOT include StateReport**                            │
│    ├─ No leadership form (UnitImprovementPlan, etc.)           │
│    ├─ No aggregated program rows (TotalAttendance, etc.)       │
│    └─ Why? StateReport is state-level internal working doc     │
└──────────────────────────────────────────────────────────────────┘
```

---

## What National Sees vs What State Keeps

### NATIONAL SEES (via GetNationalReportsAsync):

```csharp
List<Report> nationalReports = await GetNationalReportsAsync(actor, cycleId);

// Each Report contains:
foreach (var report in nationalReports)
{
    Console.WriteLine($"Report ID: {report.Id}");
    Console.WriteLine($"Unit: {report.UnitId}");
    Console.WriteLine($"State: {report.StateId}");
    Console.WriteLine($"Status: {report.Status}"); // SubmittedToNational or Acknowledged

    // Department data (original unit submissions)
    foreach (var dept in report.DepartmentReports)
    {
        Console.WriteLine($"  {dept.Department}: {dept.ReportData}"); // JSON payload
        Console.WriteLine($"    Attendance: {dept.AttendanceCount}");
        Console.WriteLine($"    Beneficiaries: {dept.BeneficiaryCount}");
    }

    // Notes from previous levels
    Console.WriteLine($"Presidential Notes: {report.PresidentialNotes}");
    Console.WriteLine($"State Notes: {report.StateNotes}");

    // Timeline of actions
    foreach (var log in report.ActivityLogs)
    {
        Console.WriteLine($"  [{log.ActionAt}] {log.Action}");
    }
}

// National DOES NOT see StateReport
// StateReport is NOT joined/included in the query above!
```

### STATE KEEPS (StateReport - internal):

```csharp
StateReport stateReport = await GetStateReportAsync(actor, stateId, cycleId);

// State leadership has this separate document:
Console.WriteLine($"StateReport ID: {stateReport.Id}");
Console.WriteLine($"Leadership Plan: {stateReport.UnitImprovementPlan}");
Console.WriteLine($"Challenges: {stateReport.ChallengesFaced}");
Console.WriteLine($"National Support Needed: {stateReport.NationalSupportNeeded}");
Console.WriteLine($"Other Notes: {stateReport.OtherNotes}");

// Aggregated metrics and programs (for state's eyes)
Console.WriteLine($"Total Units: {stateReport.TotalUnitReportsCount}");
Console.WriteLine($"Units Attended: {stateReport.UnitsAttendedTo}");
foreach (var program in stateReport.Programs)
{
    Console.WriteLine($"  {program.ProgramName}: " +
        $"Attendance={program.TotalAttendance}, " +
        $"Auto={program.IsAutoAggregated}");
}

// StateReport is NOT transmitted to national
// National never sees these fields or aggregations
```

---

## Current Architecture: Two Separate Report Paths

```
╔════════════════════════════════════════════════════════════════════════════╗
║                           UNIT REPORT (Report table)                       ║
║                                                                            ║
║  ✓ Goes to Unit President                                                 ║
║  ✓ Goes to State Leadership (as approval step)                            ║
║  ✓ Goes to National Leadership (final view)                               ║
║  ✓ Contains department JSON payloads and extracted metrics                 ║
║  ✓ Has notes from each approval level (PresidentialNotes, StateNotes)    ║
║  ✗ Does NOT include state leadership's internal commentary                ║
╚════════════════════════════════════════════════════════════════════════════╝
                                    ▲
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
        ┌───────────┴──────────┐      ┌────────────┴────────────┐
        │                      │      │                         │
        ▼                      ▼      ▼                         ▼

Unit President          State Leadership            National Leadership
├─ Reviews Report      ├─ Reviews Report           ├─ Views Report
└─ Adds notes          ├─ Adds StateNotes          ├─ Views DeptReports
                       ├─ Forwards to National     ├─ Views all notes
                       │                           ├─ Acknowledges
                       │  Also Manages:            └─ Archives
                       │  ├─ StateReport (separate!)
                       │  ├─ Leadership form
                       │  ├─ Aggregations
                       │  └─ UnitImprovementPlan
                       │     (State keeps internally)
                       └─ NOT sent to National


╔════════════════════════════════════════════════════════════════════════════╗
║                      STATE REPORT (StateReport table)                      ║
║                                                                            ║
║  ✗ Does NOT go to Unit President                                          ║
║  ✓ Stays with State Leadership (internal working document)                ║
║  ✗ Does NOT go to National (not included in GetNationalReportsAsync)      ║
║  ✓ Contains leadership commentary, plans, escalation requests             ║
║  ✓ Contains aggregated metrics and program summaries                      ║
║  PURPOSE: State-level analysis and oversight tool                         ║
╚════════════════════════════════════════════════════════════════════════════╝
```

---

## Code Evidence: Where National Gets Data

### GetNationalReportsAsync() - What's Included:

```csharp
public async Task<List<Report>> GetNationalReportsAsync(AuthContext actor, int? cycleId = null, CancellationToken ct = default)
{
    if (!_access.IsNationalLeadership(actor))
        throw new UnauthorizedAccessException("Only national leadership can view national report board.");

    var selectedCycleId = await ResolveCycleIdAsync(cycleId, ct);
    if (selectedCycleId is null)
        return [];

    // ONLY queries Report table with DepartmentReports
    // NO StateReport join
    // NO StateReportProgram join
    return await _db.Reports
        .Include(r => r.Cycle)                    // ✓ Cycle info
        .Include(r => r.DepartmentReports)        // ✓ Department data
        // MISSING: .Include(r => r.StateReport) ← This doesn't exist!
        .Where(r => r.CycleId == selectedCycleId.Value)
        .OrderBy(r => r.StateId)
        .ThenBy(r => r.UnitId)
        .ToListAsync(ct);
}
```

**Key observation:** StateReport is NOT part of the Report navigation. They are completely separate entities:
- Report → StateReport is a ONE-TO-NONE relationship (StateReport is optional, state-only)
- National only sees Report, never StateReport

---

## What National Receives in the Final Report

```
National Report View (for 1 unit report):

┌─────────────────────────────────────────────────────────────────┐
│ Report (Unit-level Master)                                      │
│                                                                 │
│ ID: 9001                                                        │
│ UnitId: 42, StateId: 7                                          │
│ Status: SubmittedToNational  ◄─ After state approves           │
│                                                                 │
│ ┌─ DepartmentReports (Original unit submissions):             │
│ │  ├─ Taleem: {"sessions":5, "attendance":120, ...}           │
│ │  ├─ Welfare: {"programs":2, "beneficiaries":30, ...}        │
│ │  ├─ Health: {"attendance":45, "beneficiaries":10, ...}      │
│ │  ├─ Finance: {"dues_collected":250.00, ...}                 │
│ │  └─ Sport: {"attendance":60, ...}                           │
│ │                                                              │
│ ├─ PresidentialNotes:                                         │
│ │  "Report looks good. Submitted for state review."          │
│ │                                                              │
│ ├─ StateNotes:  ◄─ Comments from state leadership            │
│ │  "Confirmed all numbers. Ready for national."              │
│ │                                                              │
│ ├─ NationalNotes:                                             │
│ │  (Empty until National reviews)                             │
│ │                                                              │
│ └─ ActivityLogs:                                              │
│    [2025-05-10 12:00] DepartmentReportSubmitted (Taleem)     │
│    [2025-05-10 14:30] DepartmentReportSubmitted (Welfare)    │
│    [2025-05-11 09:00] ReportSubmittedToPresident             │
│    [2025-05-12 10:00] ApprovedByUnitLeadership               │
│    [2025-05-12 14:00] ApprovedByStateLeadership              │
│    [2025-05-12 15:00] StateNotes: "Confirmed all numbers..."│
│                                                              │
└─────────────────────────────────────────────────────────────────┘

National DOES NOT see:
❌ StateReport.UnitImprovementPlan
❌ StateReport.ChallengesFaced
❌ StateReport.NationalSupportNeeded
❌ StateReport.OtherNotes
❌ StateReport.Programs (aggregated program summaries)
❌ StateReport.IsAggregated, LastAggregatedAt (metadata)

These remain STATE-ONLY internal documents.
```

---

## Summary: Two Separate Chains

| Aspect | **Unit Report (Report)** | **State Report (StateReport)** |
|--------|---|---|
| **Audience** | Unit → State → National | State leadership only |
| **Contains** | DepartmentReports (JSON) + notes | Leadership form + aggregations |
| **Progression** | Draft → SubmittedToPresident → SubmittedToState → SubmittedToNational → Acknowledged | Draft → (never submitted to National) |
| **National Sees?** | ✓ YES (via GetNationalReportsAsync) | ✗ NO (separate entity) |
| **Purpose** | Approval workflow across all levels | State analysis & decision tool |
| **Edit Lock** | After SubmittedToNational (Acknowledged) | Can be edited until state submits unit reports |

---

## If National SHOULD See State Commentary (Enhancement)

If you want National to see the state leadership's commentary, you would need:

```csharp
// FUTURE: Option to include StateReport in national view
public async Task<List<ReportWithStateContext>> GetNationalReportsWithStateAsync(
    AuthContext actor, int? cycleId = null, CancellationToken ct = default)
{
    // Would need to:
    // 1. Join Report with StateReport
    // 2. Include StateReport.Programs
    // 3. Create a DTO combining Report + StateReport data
    // 4. Return composite view to national

    // Currently NOT implemented - StateReport stays with state
}
```

---

## Current Answer to Your Question

**Q: Does National get to see the leadership form StateReport?**

**A:** No, not currently. 

- **StateReport (with leadership form)** = State-only internal document
- **Report (unit submissions)** = What goes to National
- National sees the unit department data, notes from each approval level, but NOT the state's internal commentary or aggregations

**Why?** 
- Separation of concerns: State leadership keeps a working document for analysis
- Report chain is simpler: just original unit data + approval notes
- Prevents "feedback inflation": National sees original data, not state's interpretation

**If you want National to see State commentary:**
- Create a composite view or API that joins Report + StateReport
- Pass state notes to national explicitly (in StateReport submission step)
- Or add a "StateReport Summary" field to Report that state fills in before approval

Would you like me to implement an enhancement so National CAN see the state leadership commentary?
