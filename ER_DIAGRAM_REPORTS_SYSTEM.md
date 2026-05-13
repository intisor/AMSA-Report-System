# Entity Relationship Diagram - Reports System

## High-Level ER Diagram (Mermaid)

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
        int UnitId FK
        int StateId FK
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
        int CycleId FK_denorm
        string Department
        string ReportData "JSON"
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
        int ActionByMemberId FK
        string Action
        string Notes
        datetime ActionAt
    }

    STATE_REPORT {
        int Id PK
        int StateId FK
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

## Detailed Schema Visualization

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         REPORTING_CYCLE                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│ PK: Id (int)                                                               │
│ ──────────────────────────                                                 │
│    CycleMonth (string)              ◄─── "January 2025"                    │
│    StartDate (datetime)                                                     │
│    EndDate (datetime)                                                       │
│    SubmissionDeadline (datetime)                                            │
│    IsLocked (bit)                                                           │
│    CreatedAt (datetime)                                                     │
│    UpdatedAt (datetime)                                                     │
│                                                                             │
│ NAVIGATION:                                                                 │
│    Reports (1:N) ──────────────────┬─────────────────────────┐            │
│    StateReports (1:N) ─────────────┤                         │            │
└─────────────────────────────────────┼─────────────────────────┼─────────────┘
                                      │                         │
                ┌─────────────────────┘                         └──────────────────┐
                ▼                                                                  ▼

┌──────────────────────────────────────────────────┐    ┌──────────────────────────────────────┐
│            REPORT (Unit-level Master)            │    │      STATE_REPORT                   │
├──────────────────────────────────────────────────┤    ├──────────────────────────────────────┤
│ PK: Id (int)                                     │    │ PK: Id (int)                         │
│ FK: CycleId ──────► ReportingCycle               │    │ FK: CycleId ──► ReportingCycle       │
│ ────────────────────                             │    │ ──────────────────                   │
│    UnitId (int)        ◄─── Which unit?          │    │    StateId (int)      ◄─ Which state?
│    StateId (int)       ◄─── Which state?         │    │    Status (ReportStatus)             │
│    Status (ReportStatus)                         │    │                                      │
│    IsCompliant (bit)                             │    │ AGGREGATED METRICS:                  │
│                                                  │    │    UnitsAttendedTo (int)             │
│ SUBMISSION TRACKING (Unit→President):           │    │    TotalUnitReportsCount (int)       │
│    SubmittedToPresidentAt (datetime)            │    │    UnitPerformanceRating (int)       │
│    SubmittedByMemberId (int)                    │    │                                      │
│                                                  │    │ LEADERSHIP FORM (State Internal):   │
│ UNIT PRESIDENT APPROVAL:                        │    │    UnitImprovementPlan (string)      │
│    ApprovedByPresidentAt (datetime)             │    │    ChallengesFaced (string)          │
│    ApprovedByPresidentMemberId (int)            │    │    NationalSupportNeeded (string)    │
│    PresidentialNotes (string)                   │    │    OtherNotes (string)               │
│                                                  │    │                                      │
│ STATE LEADERSHIP APPROVAL (President→State):    │    │ STATE PRESIDENT APPROVAL:            │
│    ApprovedByStateAt (datetime)                 │    │    PresidentialNote (string)         │
│    ApprovedByStateMemberId (int)                │    │    ApprovedByPresidentAt (datetime)  │
│    StateNotes (string)                          │    │    ApprovedByPresidentMemberId (int) │
│                                                  │    │                                      │
│ NATIONAL ACKNOWLEDGMENT (State→National):       │    │ AGGREGATION METADATA (NEW):          │
│    AcknowledgedByNationalAt (datetime)          │    │    IsAggregated (bit)                │
│    AcknowledgedByNationalMemberId (int)         │    │    LastAggregatedAt (datetime)       │
│    NationalNotes (string)                       │    │    LastAggregatedByMemberId (int)    │
│                                                  │    │                                      │
│ TIMESTAMPS:                                      │    │ TIMESTAMPS:                          │
│    CreatedAt (datetime)                         │    │    CreatedAt (datetime)              │
│    UpdatedAt (datetime)                         │    │    UpdatedAt (datetime)              │
│                                                  │    │                                      │
│ NAVIGATION:                                      │    │ NAVIGATION:                          │
│    Cycle ─► ReportingCycle (1:1)               │    │    Cycle ─► ReportingCycle (1:1)    │
│    DepartmentReports (1:N) ──┐                 │    │    Programs (1:N) ──┐                │
│    ActivityLogs (1:N) ────────┼─────┐          │    │                     │                │
└──────────────────────────────┼─────┼──────────┘    └──────────────────────┼────────────────┘
                               │     │                                       │
                        ┌──────┘     │                          ┌────────────┘
                        │            │                          │
                        ▼            ▼                          ▼

    ┌───────────────────────────────────┐    ┌──────────────────────────────────────┐
    │   DEPARTMENT_REPORT               │    │  STATE_REPORT_PROGRAM                │
    ├───────────────────────────────────┤    ├──────────────────────────────────────┤
    │ PK: Id (int)                      │    │ PK: ProgramId (int)                  │
    │ FK: ReportId ─► Report (1:1)      │    │ FK: StateReportId ─► StateReport     │
    │                                   │    │                                      │
    │ IDENTIFICATION:                   │    │ DATA:                                │
    │    Department (DepartmentType)    │    │    ProgramName (string)              │
    │    CycleId (int) [denormalized]   │    │    Objectives (string)               │
    │                                   │    │    Outcomes (string)                 │
    │ DATA STORAGE:                     │    │    TotalAttendance (int)             │
    │    ReportData (string - JSON) ◄──┼────┼─ Aggregated from DepartmentReports   │
    │       └─ Full department form     │    │    TotalBeneficiaries (int)          │
    │                                   │    │                                      │
    │ EXTRACTED ANALYTICS (Hybrid):     │    │ AGGREGATION FLAG (NEW):              │
    │    SessionsOrganized (int)        │    │    IsAutoAggregated (bit) ◄──┐      │
    │    AttendanceCount (int)          │    │       └─ TRUE = auto-computed        │
    │    TotalMemberCount (int)         │    │       └─ FALSE = manual edit         │
    │    HasOnCampusActivity (bit)      │    │                                      │
    │    ProgramCount (int)             │    │ Used in aggregation to:              │
    │    MemberParticipantCount (int)   │    │    ✓ Preserve manual program rows    │
    │    DuesCollected (decimal)        │    │    ✓ Replace only auto rows          │
    │    ExpectedDues (decimal)         │    └──────────────────────────────────────┘
    │    BeneficiaryCount (int)         │
    │                                   │
    │ SUBMISSION STATUS:                │    
    │    IsSubmitted (bit)              │
    │    SubmittedAt (datetime)         │
    │    SubmittedByMemberId (int)      │
    │    IsCompliant (bit)              │
    │                                   │
    │ TIMESTAMPS:                       │
    │    CreatedAt (datetime)           │
    │    UpdatedAt (datetime)           │
    └───────────────────────────────────┘

                    ┌──────────────────────────────────────┐
                    │   REPORT_ACTIVITY_LOG                │
                    ├──────────────────────────────────────┤
                    │ PK: Id (int)                         │
                    │ FK: ReportId ─► Report (1:1)         │
                    │                                      │
                    │ AUDIT DATA:                          │
                    │    Action (string)                   │
                    │       ├─ "ReportCreated"             │
                    │       ├─ "DepartmentReportSubmitted"  │
                    │       ├─ "ReportSubmittedToPresident" │
                    │       ├─ "ApprovedByUnitLeadership"   │
                    │       ├─ "ApprovedByStateLeadership"  │
                    │       └─ "AcknowledgedByNational"     │
                    │    Notes (string) [optional]         │
                    │    ActionByMemberId (int) [who]      │
                    │    ActionAt (datetime) [when]        │
                    └──────────────────────────────────────┘
```

---

## Cardinality & Relationships Summary

```
ReportingCycle (1) ──────────────────────┐
                                         │
                                    (N) Report
                                         │
                            Contains (N) DepartmentReport
                            Contains (N) ReportActivityLog

ReportingCycle (1) ──────────────────────┐
                                         │
                                    (N) StateReport
                            Aggregates (N) StateReportProgram

DepartmentReport Data Flow:
┌──────────────┐    ┌──────────────────────┐    ┌────────────────────┐
│ DepartmentReport (from Unit submissions)    │    │ StateReportProgram │
│ - AttendanceCount, Beneficiaries, etc.      │    │ (aggregated)       │
└──────────────┴──────────────────────────────┘    └────────────────────┘
                        │
                        │ (grouped by Department)
                        │ (summed: TotalAttendance, TotalBeneficiaries)
                        ▼
                StateReportProgram rows created with IsAutoAggregated=TRUE
                BUT: rows with IsAutoAggregated=FALSE are PRESERVED
```

---

## Example: Single Unit Report with All Relationships

```
ReportingCycle
├─ Id: 202406
├─ CycleMonth: "June 2024"
└─ ...

    ▼

Report
├─ Id: 9001
├─ UnitId: 42
├─ StateId: 7
├─ CycleId: 202406 ◄─── FK to ReportingCycle
├─ Status: "SubmittedToState"
├─ DepartmentReports: [
│   {
│     Id: 101
│     ReportId: 9001 ◄─── FK to Report
│     Department: Taleem
│     ReportData: '{"sessions":5, "attendance":120, ...}'
│     AttendanceCount: 120
│     IsSubmitted: true
│     IsAutoAggregated: N/A (attribute is on StateReportProgram, not here)
│   },
│   {
│     Id: 102
│     ReportId: 9001
│     Department: Welfare
│     ReportData: '{"programs":2, "beneficiaries":30, ...}'
│     BeneficiaryCount: 30
│     IsSubmitted: true
│   },
│   ...more departments
│ ]
├─ ActivityLogs: [
│   {
│     Id: 1001
│     ReportId: 9001 ◄─── FK to Report
│     Action: "ReportCreated"
│     ActionAt: 2024-06-10T10:00:00Z
│   },
│   {
│     Id: 1002
│     ReportId: 9001
│     Action: "DepartmentReportSubmitted"
│     Notes: "Updated Taleem report"
│     ActionAt: 2024-06-10T12:30:00Z
│   },
│   ...more activity
│ ]
└─ Cycle: ─────────► ReportingCycle (navigation to cycle 202406)

    + 

StateReport
├─ Id: 1200
├─ StateId: 7 ◄─── Same state as Report.StateId
├─ CycleId: 202406 ◄─── Same cycle as Report.CycleId, FK to ReportingCycle
├─ Status: "Draft"
├─ UnitsAttendedTo: 5 ◄─── Count of units in state with submitted reports
├─ TotalUnitReportsCount: 8 ◄─── Total unit reports for this state/cycle
├─ UnitImprovementPlan: "Need to train 3 more unit leaders"
├─ ChallengesFaced: "Attendance drop in summer months"
├─ Programs: [
│   {
│     ProgramId: 5001
│     StateReportId: 1200 ◄─── FK to StateReport
│     ProgramName: "Taleem"
│     TotalAttendance: 450 ◄─ SUM of all unit Taleem.AttendanceCount
│     TotalBeneficiaries: 0
│     IsAutoAggregated: TRUE ◄─── Auto-computed from units
│   },
│   {
│     ProgramId: 5002
│     StateReportId: 1200
│     ProgramName: "Welfare"
│     TotalAttendance: 0
│     TotalBeneficiaries: 85 ◄─ SUM of all unit Welfare.BeneficiaryCount
│     IsAutoAggregated: TRUE
│   },
│   {
│     ProgramId: 5004
│     StateReportId: 1200
│     ProgramName: "State Initiative"
│     TotalAttendance: 50
│     TotalBeneficiaries: 25
│     IsAutoAggregated: FALSE ◄─── Manually created by state leader
│                                      (PRESERVED across aggregations)
│   }
│ ]
└─ Cycle: ─────────► ReportingCycle (navigation to cycle 202406)

RELATIONSHIP SUMMARY:
├─ Report → ReportingCycle (1:1, required)
├─ Report → DepartmentReport (1:N, owned)
├─ Report → ReportActivityLog (1:N, owned)
├─ DepartmentReport → Report (N:1, foreign key)
├─ StateReport → ReportingCycle (1:1, required)
├─ StateReport → StateReportProgram (1:N, owned)
├─ StateReportProgram → StateReport (N:1, foreign key)
├─ Report ≠ StateReport (NO DIRECT LINK, separate entities)
└─ Report.StateId & StateReport.StateId refer to same State (soft FK)
```

---

## Key Points About Relationships

```
1. REPORT TABLE (Unit-level, flows to National)
   ├─ Parent: ReportingCycle (1:1)
   ├─ Children: DepartmentReports (1:N) ─ the 9 departments
   ├─ Children: ActivityLogs (1:N) ─ approval timeline
   └─ Goes through: Unit → State → National

2. DEPARTMENT_REPORT TABLE (Granular department data)
   ├─ Parent: Report (N:1) ─ one report has many departments
   ├─ Hybrid Storage: ReportData (JSON) + extracted fields (AttendanceCount, etc.)
   ├─ Submission Flag: IsSubmitted (per department)
   └─ Analytics: DuesCollected, BeneficiaryCount for aggregation

3. STATE_REPORT TABLE (State-level, stays internal to state)
   ├─ Parent: ReportingCycle (1:1)
   ├─ Children: StateReportProgram (1:N) ─ aggregated programs
   ├─ Leadership Form: UnitImprovementPlan, ChallengesFaced, etc.
   ├─ Metrics: UnitsAttendedTo, UnitPerformanceRating (computed)
   └─ DOES NOT go to National (separate from Report flow)

4. STATE_REPORT_PROGRAM TABLE (Aggregations, new IsAutoAggregated flag)
   ├─ Parent: StateReport (N:1) ─ one state report has many programs
   ├─ Aggregated from: DepartmentReport rows (grouped by Department)
   ├─ IsAutoAggregated=TRUE: created by RecomputeStateAggregateAsync
   ├─ IsAutoAggregated=FALSE: created manually by state leader (preserved!)
   └─ Auto rows are refreshed on each aggregation run

5. REPORT_ACTIVITY_LOG TABLE (Audit trail)
   ├─ Parent: Report (N:1) ─ one report has many log entries
   ├─ Timeline: Shows all actions (created, submitted, approved, rejected, etc.)
   └─ Includes: Action type, actor (MemberId), timestamp, optional notes

NO DIRECT LINK:
Report and StateReport do NOT have a foreign key relationship.
They are separate aggregation paths that share the same StateId/CycleId values.
```

---

## SQL DDL Summary

```sql
-- ReportingCycle: Define reporting periods
CREATE TABLE ReportingCycles (
    Id INT PRIMARY KEY,
    CycleMonth NVARCHAR(50),
    StartDate DATETIME,
    EndDate DATETIME,
    SubmissionDeadline DATETIME,
    IsLocked BIT,
    CreatedAt DATETIME,
    UpdatedAt DATETIME
);

-- Report: Unit-level master report (flows to national)
CREATE TABLE Reports (
    Id INT PRIMARY KEY,
    UnitId INT,
    StateId INT,
    CycleId INT FOREIGN KEY REFERENCES ReportingCycles(Id),
    Status NVARCHAR(50),
    IsCompliant BIT,
    -- Submission to President
    SubmittedToPresidentAt DATETIME NULL,
    SubmittedByMemberId INT NULL,
    -- Presidential approval
    ApprovedByPresidentAt DATETIME NULL,
    ApprovedByPresidentMemberId INT NULL,
    PresidentialNotes NVARCHAR(MAX) NULL,
    -- State approval
    ApprovedByStateAt DATETIME NULL,
    ApprovedByStateMemberId INT NULL,
    StateNotes NVARCHAR(MAX) NULL,
    -- National acknowledgment
    AcknowledgedByNationalAt DATETIME NULL,
    AcknowledgedByNationalMemberId INT NULL,
    NationalNotes NVARCHAR(MAX) NULL,
    CreatedAt DATETIME,
    UpdatedAt DATETIME
);

-- DepartmentReport: Individual department data (9 per Report)
CREATE TABLE DepartmentReports (
    Id INT PRIMARY KEY,
    ReportId INT FOREIGN KEY REFERENCES Reports(Id),
    CycleId INT,
    Department NVARCHAR(50),
    ReportData NVARCHAR(MAX), -- JSON
    -- Extracted analytics (hybrid model)
    SessionsOrganized INT NULL,
    AttendanceCount INT NULL,
    TotalMemberCount INT NULL,
    HasOnCampusActivity BIT NULL,
    ProgramCount INT NULL,
    MemberParticipantCount INT NULL,
    DuesCollected DECIMAL(18,2) NULL,
    ExpectedDues DECIMAL(18,2) NULL,
    BeneficiaryCount INT NULL,
    IsSubmitted BIT,
    IsCompliant BIT,
    SubmittedAt DATETIME NULL,
    SubmittedByMemberId INT NULL,
    CreatedAt DATETIME,
    UpdatedAt DATETIME
);

-- ReportActivityLog: Audit trail
CREATE TABLE ReportActivityLogs (
    Id INT PRIMARY KEY,
    ReportId INT FOREIGN KEY REFERENCES Reports(Id),
    ActionByMemberId INT,
    Action NVARCHAR(255),
    Notes NVARCHAR(MAX) NULL,
    ActionAt DATETIME
);

-- StateReport: State-level aggregation (stays internal to state)
CREATE TABLE StateReports (
    Id INT PRIMARY KEY,
    StateId INT,
    CycleId INT FOREIGN KEY REFERENCES ReportingCycles(Id),
    Status NVARCHAR(50),
    -- Metrics
    UnitsAttendedTo INT,
    TotalUnitReportsCount INT,
    UnitPerformanceRating INT NULL,
    -- Leadership form fields
    UnitImprovementPlan NVARCHAR(MAX) NULL,
    ChallengesFaced NVARCHAR(MAX) NULL,
    NationalSupportNeeded NVARCHAR(MAX) NULL,
    OtherNotes NVARCHAR(MAX) NULL,
    -- Presidential approval (state-level)
    PresidentialNote NVARCHAR(MAX) NULL,
    ApprovedByPresidentAt DATETIME NULL,
    ApprovedByPresidentMemberId INT NULL,
    -- National acknowledgment (state-level)
    NationalNote NVARCHAR(MAX) NULL,
    AcknowledgedByNationalAt DATETIME NULL,
    AcknowledgedByNationalMemberId INT NULL,
    -- Submission
    SubmittedAt DATETIME NULL,
    SubmittedByMemberId INT NULL,
    -- Aggregation metadata (NEW)
    IsAggregated BIT,
    LastAggregatedAt DATETIME NULL,
    LastAggregatedByMemberId INT NULL,
    CreatedAt DATETIME,
    UpdatedAt DATETIME
);

-- StateReportProgram: Aggregated program rows (auto + manual)
CREATE TABLE StateReportPrograms (
    ProgramId INT PRIMARY KEY,
    StateReportId INT FOREIGN KEY REFERENCES StateReports(Id),
    ProgramName NVARCHAR(255),
    Objectives NVARCHAR(MAX) NULL,
    Outcomes NVARCHAR(MAX) NULL,
    TotalAttendance INT NULL,
    TotalBeneficiaries INT NULL,
    -- Aggregation flag (NEW)
    IsAutoAggregated BIT DEFAULT 1
);
```

---

## Visual Flow: Data Path Through System

```
UNIT LEVEL:
───────────
Department Officer fills DepartmentReport JSON
                ▼
        Unit collects 5-9 DepartmentReports
                ▼
        Report.Status = Draft
                ▼
        Unit President reviews & approves
                ▼
        Report.Status = SubmittedToPresident
        ActivityLog: "ReportSubmittedToPresident"
                │
                ▼

STATE LEVEL:
───────────
State Leadership receives Report
                ▼
        Gets all Reports for StateId=7, CycleId=202406
                ▼
        Creates StateReport (separate entity!)
                ▼
        RecomputeStateAggregateAsync() runs:
        ├─ Groups DepartmentReports by Department
        ├─ Sums TotalAttendance, TotalBeneficiaries
        ├─ Removes old auto StateReportProgram rows (IsAutoAggregated=TRUE)
        ├─ Adds new aggregated StateReportProgram rows (IsAutoAggregated=TRUE)
        └─ Preserves manual StateReportProgram rows (IsAutoAggregated=FALSE)
                ▼
        State Leader adds UnitImprovementPlan, ChallengesFaced
                ▼
        State Leader approves Report
                ▼
        Report.Status = SubmittedToState
        Report.ApprovedByStateAt, StateNotes set
        ActivityLog: "ApprovedByStateLeadership"
                │
                ├─► StateReport stays with STATE (not sent to national)
                │
                ▼

NATIONAL LEVEL:
───────────────
National Leadership calls GetNationalReportsAsync()
                ▼
        Retrieves Report + DepartmentReports + ActivityLogs
        (does NOT retrieve StateReport)
                ▼
        National reviews original unit data
        + PresidentialNotes (from unit president)
        + StateNotes (from state leadership)
                ▼
        Report.Status = SubmittedToNational
        Report.AcknowledgedByNationalAt, NationalNotes set
        ActivityLog: "AcknowledgedByNational"
                ▼
        Report.Status = Acknowledged
        (LOCKED for edits)

STATE KEEPS INTERNAL:
─────────────────────
StateReport {
    UnitImprovementPlan,
    ChallengesFaced,
    NationalSupportNeeded,
    OtherNotes,
    StateReportProgram rows (aggregated)
}
National NEVER sees these.
```

This completes the full ER diagram with all relationships and data flows! 📊
