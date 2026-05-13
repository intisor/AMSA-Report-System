# Entity Relationship Diagram - Reports System (CORRECTED)

## High-Level ER Diagram (Mermaid) - FIXED

```
erDiagram
    REPORTING_CYCLE ||--o{ REPORT : "has many"
    REPORTING_CYCLE ||--o{ STATE_REPORT : "has many"
    REPORT ||--o{ DEPARTMENT_REPORT : "contains"
    REPORT ||--o{ REPORT_ACTIVITY_LOG : "has"
    STATE_REPORT ||--o{ STATE_REPORT_PROGRAM : "aggregates"

    REPORTING_CYCLE {
        int Id PK
        string CycleMonth
        datetime StartDate
        datetime EndDate
        datetime SubmissionDeadline
        bit IsLocked
        datetime CreatedAt
        datetime UpdatedAt
    }

    REPORT {
        int Id PK
        int UnitId
        int StateId
        int CycleId FK
        string Status
        bit IsCompliant
        datetime SubmittedToPresidentAt
        int SubmittedByMemberId
        datetime ApprovedByPresidentAt
        int ApprovedByPresidentMemberId
        string PresidentialNotes
        datetime ApprovedByStateAt
        int ApprovedByStateMemberId
        string StateNotes
        datetime AcknowledgedByNationalAt
        int AcknowledgedByNationalMemberId
        string NationalNotes
        datetime CreatedAt
        datetime UpdatedAt
    }

    DEPARTMENT_REPORT {
        int Id PK
        int ReportId FK
        int CycleId
        string Department
        string ReportData
        int SessionsOrganized
        int AttendanceCount
        int TotalMemberCount
        bit HasOnCampusActivity
        int ProgramCount
        int MemberParticipantCount
        decimal DuesCollected
        decimal ExpectedDues
        int BeneficiaryCount
        bit IsSubmitted
        bit IsCompliant
        datetime SubmittedAt
        int SubmittedByMemberId
        datetime CreatedAt
        datetime UpdatedAt
    }

    REPORT_ACTIVITY_LOG {
        int Id PK
        int ReportId FK
        int ActionByMemberId
        string Action
        string Notes
        datetime ActionAt
    }

    STATE_REPORT {
        int Id PK
        int StateId
        int CycleId FK
        string Status
        int UnitsAttendedTo
        int TotalUnitReportsCount
        int UnitPerformanceRating
        string UnitImprovementPlan
        string ChallengesFaced
        string NationalSupportNeeded
        string OtherNotes
        string PresidentialNote
        datetime ApprovedByPresidentAt
        int ApprovedByPresidentMemberId
        string NationalNote
        datetime AcknowledgedByNationalAt
        int AcknowledgedByNationalMemberId
        datetime SubmittedAt
        int SubmittedByMemberId
        bit IsAggregated
        datetime LastAggregatedAt
        int LastAggregatedByMemberId
        datetime CreatedAt
        datetime UpdatedAt
    }

    STATE_REPORT_PROGRAM {
        int ProgramId PK
        int StateReportId FK
        string ProgramName
        string Objectives
        string Outcomes
        int TotalAttendance
        int TotalBeneficiaries
        bit IsAutoAggregated
    }
```

---

## MISMATCH FIXES

**What was wrong:**

1. ❌ REPORT: `int UnitId FK`, `int StateId FK` 
   - ✅ FIXED: `int UnitId`, `int StateId` (data fields, not FKs)

2. ❌ DEPARTMENT_REPORT: `int CycleId FK_denorm`
   - ✅ FIXED: `int CycleId` (denormalized data field, not FK)

3. ❌ REPORT_ACTIVITY_LOG: `int ActionByMemberId FK`
   - ✅ FIXED: `int ActionByMemberId` (soft reference, not FK)

4. ❌ STATE_REPORT: `int StateId FK`
   - ✅ FIXED: `int StateId` (data field, not FK)

---

## Correct Foreign Keys Only

```
TRUE FOREIGN KEYS (Enforced by DB):
├─ Report.CycleId ────► ReportingCycle.Id
├─ DepartmentReport.ReportId ─► Report.Id
├─ ReportActivityLog.ReportId ─► Report.Id
├─ StateReport.CycleId ───► ReportingCycle.Id
└─ StateReportProgram.StateReportId ─► StateReport.Id

SOFT REFERENCES (Data fields only, no FK constraint):
├─ Report.UnitId ──────► identifies which unit (no FK)
├─ Report.StateId ─────► identifies which state (no FK)
├─ DepartmentReport.CycleId ──► denormalized (no FK)
├─ ReportActivityLog.ActionByMemberId ─► identifies who (no FK)
└─ StateReport.StateId ──► identifies which state (no FK)
```

This distinction prevents accidental cascade deletes and clarifies schema intent!

Committed in branch: `codex/refine-report-json-migration`
