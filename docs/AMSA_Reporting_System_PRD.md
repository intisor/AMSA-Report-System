# AMSA Nigeria Reporting System

**Product Requirements Document**  
**Version:** 1.0.0  
**Date:** March 2026  
**Status:** Draft – For Review

**Platform:** Blazor Server / ASP.NET / SQL Server  
**Auth Layer:** AMSA API (JWT / Scope-based)

---

## 1. Executive Summary

The AMSA Nigeria Reporting System is a digitised monthly activity reporting platform built for the Ahmadiyya Muslim Students' Association Nigeria. It replaces the current manual workflow – where department officers fill Microsoft Word documents and submit them via WhatsApp/email chains – with a structured, web-based system that captures all report data in a database, enforces compliance thresholds, routes reports through the organisational hierarchy, and gives National leadership real-time visibility into activity across all units and states.

The system is built as a Blazor Server application backed by ASP.NET and SQL Server. It consumes the existing AMSA API as its authentication and member data layer, registering as an external app and obtaining JWT tokens with scope-based and role-based claims.

### Core Value Proposition

One structured monthly submission per unit (9 department sections) + one per state feeds a live national dashboard. No more chasing Word docs. No more manual collation. No more missing reports. Compliance flags are automatic.

---

## 2. Problem Statement

### 2.1 Current State

Every month, each AMSA unit must report on 9 departmental areas (Taleem, Tabligh, Welfare, Sport, Finance, Health, Secondary School, Tajneed, General). The current process is:

1. Each department officer fills a Word document template manually
2. The completed document is sent to the Unit President via WhatsApp or email
3. The Unit President reviews, adds a note, and forwards to the State General Secretary
4. The State GS manually collates reports from every unit in their state
5. The State GS then submits the state's own monthly report to the National GS
6. The National GS receives dozens of Word documents and manually aggregates them

### 2.2 Pain Points

| Pain Point | Impact | Who Feels It |
|---|---|---|
| Lost/missing reports | No record of unit activity for that month | National GS, State GS |
| Late submissions | National cannot compile accurate monthly summary | National President |
| No submission visibility | State GS cannot see who has/hasn't submitted | State GS |
| No compliance tracking | Taleem 75% threshold violations go unnoticed | National Education Sec. |
| Manual aggregation | National GS spends hours combining Word docs | National GS |
| No trend analysis | Cannot identify consistently underperforming units | National President |
| Format inconsistency | Officers fill fields incorrectly or incompletely | All reviewers |
| No escalation mechanism | Challenges needing National intervention get buried | State Presidents |

---

## 3. Goals & Success Metrics

### 3.1 Goals

- Digitise the monthly reporting workflow end-to-end from department officer to National GS
- Eliminate manual Word document submission and WhatsApp-based report chains
- Enforce constitutional minimum standards automatically (compliance engine)
- Give National leadership real-time visibility into unit and state performance
- Store all historical report data in a queryable database
- Send automated reminders to ensure timely submissions

### 3.2 Success Metrics

| Metric | Baseline (Current) | Target (6 months post-launch) |
|---|---|---|
| Report submission rate | ~60% (estimated) | >90% of units submit monthly |
| On-time submission rate | Unknown | >80% submitted before deadline |
| National GS collation time | ~4-6 hours/month | <30 minutes/month |
| Compliance visibility | 0% automated | 100% of threshold fields auto-flagged |
| Historical data availability | Fragmented Word files | 100% searchable in DB from go-live |
| Escalation response time | Ad-hoc | National responds to flagged items within 48hrs |

---

## 4. Stakeholders & User Personas

### 4.1 User Personas

#### Persona 1 – Department Officer (Unit Level)

- **Role:** e.g. Taleem Secretary, Welfare Secretary, Financial Secretary
- **Reports to:** Unit President
- **Primary task:** Fill and submit their department's section of the monthly report
- **Pain today:** Fills a Word doc, sends via WhatsApp, doesn't know if it was received
- **Needs from system:** 
  - Simple form for their department only
  - Knows when their section is submitted
  - Auth scope: `read:members`, submit their department section only

#### Persona 2 – Unit President

- **Role:** Unit President (male or female wing)
- **Reports to:** State General Secretary
- **Primary task:** Review all 9 dept sections, add presidential note, approve and forward to State
- **Pain today:** Chasing 9 different officers for their Word docs every month
- **Needs from system:**
  - Dashboard showing which dept sections are done vs pending
  - One-click approve & forward
  - Auth scope: `read:members`, `read:organization`, approve unit reports

#### Persona 3 – State General Secretary / State President

- **Role:** State GS or State President
- **Reports to:** National GS
- **Primary task:** Monitor all unit submissions in the state, submit state-level report, forward to National
- **Pain today:** Manually collating 5-15 unit Word docs every month
- **Needs from system:**
  - State dashboard with all unit submission statuses
  - State report form
  - One-click forward
  - Auth scope: `read:members`, `read:organization`, `read:statistics` for their state

#### Persona 4 – National General Secretary

- **Role:** National General Secretary
- **Reports to:** National President
- **Primary task:** Collate all unit and state reports nationally, create reporting cycles, send reminders
- **Pain today:** Receives dozens of Word docs, manually aggregates, cannot see trends
- **Needs from system:**
  - National dashboard with full visibility
  - Create cycles
  - Flag overdue submissions
  - Export summaries
  - Auth scope: All scopes – full read access across National hierarchy

#### Persona 5 – National President / Vice President

- **Role:** National President / VP
- **Reports to:** Amir Sahib (Jama'at Nigeria)
- **Primary task:** Review national performance, identify issues, make executive decisions
- **Pain today:** Relies on GS's manual summary – no direct visibility
- **Needs from system:**
  - Executive dashboard
  - Top-level analytics
  - Compliance summary
  - Escalations view
  - Auth scope: `read:statistics`, `read:exco`, `read:organization`, `read:members`

---

## 5. System Architecture

### 5.1 High-Level Architecture

```
┌────────────────────────────────────────────────────────────────┐
│                    AMSA REPORTING SYSTEM                        │
│                  (Blazor Server + ASP.NET)                       │
├────────────────────────────────────────────────────────────────┤
│  Blazor UI Layer                                                 │
│  ┌──────────────────┐  ┌──────────────────┐  ┌────────────────┐ │
│  │  Unit Report     │  │ State Report     │  │ National       │ │
│  │  Forms           │  │ Form             │  │ Dashboard +    │ │
│  │                  │  │                  │  │ Analytics      │ │
│  └──────────────────┘  └──────────────────┘  └────────────────┘ │
├────────────────────────────────────────────────────────────────┤
│  Service Layer                                                   │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────┐  │
│  │ ReportWorkflow   │  │ ComplianceEngine │  │ ReminderSvc  │  │
│  │ Service          │  │                  │  │ (Background) │  │
│  └──────────────────┘  └──────────────────┘  └──────────────┘  │
├────────────────────────────────────────────────────────────────┤
│  Data Layer                                                      │
│  ┌──────────────────────────┐  ┌──────────────────────────────┐ │
│  │  AmsaReportingDB         │  │  AmsaDB (read-only ref)      │ │
│  │  (SQL Server)            │  │  via AMSA API                │ │
│  └──────────────────────────┘  └──────────────────────────────┘ │
├────────────────────────────────────────────────────────────────┤
│  Auth Layer                                                      │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │  AMSA API  → POST /api/auth/token  → JWT + Role Claims    │ │
│  └─────────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────┘
```

### 5.2 Technology Stack

| Layer | Technology | Rationale |
|---|---|---|
| Frontend | Blazor Server (.NET 10) | Same C# ecosystem, direct EF Core access, Copilot-friendly |
| Backend | ASP.NET Core (.NET 10) | Consistent with AMSA API, Result<T> pattern |
| Database | SQL Server (AmsaReportingDB) | Same instance as AmsaDB, enables cross-DB queries |
| ORM | Entity Framework Core 10 | Migrations, LINQ, consistent with existing codebase |
| Auth | AMSA API JWT tokens | Single source of truth for members and roles |
| Background jobs | .NET BackgroundService | Reminder scheduler, no external dependency |
| Real-time | Blazor SignalR (built-in) | Notification push, live submission status updates |

### 5.3 Database Architecture

```
SQL Server Instance
├── AmsaDB                    (existing – AMSA API manages this)
│   ├── Members
│   ├── Units
│   ├── States
│   ├── Nationals
│   ├── Departments
│   ├── Levels
│   └── AppRegistrations
│
└── AmsaReportingDB           (new – Reporting System owns this)
    ├── ReportingCycles        ← Monthly windows, deadlines
    ├── Reports                ← One per Unit per Cycle
    ├── DepartmentReports      ← One per Dept per Report
    ├── TaleemReports          ← Dept-specific fields
    ├── TablighReports
    ├── WelfareReports
    ├── SportReports
    ├── FinanceReports
    ├── HealthReports
    ├── SecondarySchoolReports
    ├── TajneedReports
    ├── GeneralReports
    ├── ReportAttachments
    ├── ReportActivityLog
    ├── StateReports           ← One per State per Cycle
    ├── StateReportPrograms    ← Normalised state programs
    ├── StateReportAttachments
    ├── StateReportActivityLog
    └── Notifications
```

---

## 6. Domain Model (UML Class Diagram)

### Core Entities

**Relationships with AMSA API (read via API):**
- `Member` (MemberId, FirstName, LastName, Mkanid, UnitId)
- `Unit` (UnitId, UnitName, StateId)
- `State` (StateId, StateName, NationalId)

**Reporting System Entities:**

#### ReportingCycle
- CycleId: int
- ReportMonth: string
- Deadline: date
- IsLocked: bool
- CreatedBy: int
- Reports: Collection<Report>
- StateReports: Collection<StateReport>

#### Report
- ReportId: int
- CycleId: int
- UnitId: int
- SubmittedBy: int
- Status: string (CREATED, DRAFT, SUBMITTED_TO_PRESIDENT, APPROVED_BY_PRESIDENT, SUBMITTED_TO_STATE, APPROVED_BY_STATE, SUBMITTED_TO_NATIONAL, ACKNOWLEDGED)
- PresidentialNote: string
- StateNote: string
- NationalNote: string
- DepartmentReports: Collection<DepartmentReport>
- ActivityLogs: Collection<ReportActivityLog>

#### DepartmentReport
- DepartmentReportId: int
- ReportId: int
- Department: string (enum: Taleem, Tabligh, Welfare, Sport, Finance, Health, SecondarySchool, Tajneed, General)
- SubmittedBy: int
- Status: string (DRAFT, SUBMITTED)
- SubmittedAt: datetime
- TaleemReport: TaleemReport? (1-to-1 per type)
- TablighReport: TablighReport?
- WelfareReport: WelfareReport?
- SportReport: SportReport?
- FinanceReport: FinanceReport?
- HealthReport: HealthReport?
- SecondarySchoolReport: SecondarySchoolReport?
- TajneedReport: TajneedReport?
- GeneralReport: GeneralReport?

#### Department Report Types

**TaleemReport**
- AvgAttendance: int
- TotalMembers: int
- SessionMins: int
- TestCount: int
- SessionsOrg: int
- IsSessionsComp: bool
- IsAttendComp: bool

**TablighReport**
- ActivitiesDetail: string
- Purpose: string
- AttendanceCount: int
- Beneficiaries: int
- HasOnCampusActivity: bool
- IsCompliant: bool

**WelfareReport**
- ProgramCount: int
- Participants: int
- ActivitiesDates: string
- BriefReport: string
- IsCompliant: bool

**SportReport**
- GamesPlayed: int
- MemberParticipants: int
- NonMemberParticipants: int
- TotalMemberCount: int
- IsAttendanceCompliant: bool

**FinanceReport**
- DuesCollected: decimal
- ExpectedDues: decimal
- DefaultReason: string
- CollectionRate: decimal
- IsFullyCollect: bool

**HealthReport**
- ActivitiesWithDates: string
- BeneficiaryCount: int

**SecondarySchoolReport**
- AttendedJamaatMtg: bool
- ParentSupportEfforts: string
- JambiteParticipation: string

**TajneedReport**
- HasAccurateTaj: bool
- ImprovEfforts: string
- AccuracyPlans: string

**GeneralReport**
- ChallengesFaced: string
- OtherActivities: string

#### StateReport
- StateReportId: int
- CycleId: int
- StateId: int
- SubmittedBy: int
- Status: string (DRAFT, SUBMITTED_TO_NATIONAL, ACKNOWLEDGED)
- UnitPresAttended: int
- TotalUnitPres: int
- PerfRating: int?
- PerfComment: string
- ChallengesFaced: string
- NatSupportNeeded: string
- Programs: Collection<StateReportProgram>
- Attachments: Collection<StateReportAttachment>
- ActivityLogs: Collection<StateReportActivityLog>

#### StateReportProgram
- ProgramId: int
- ProgramName: string
- Objectives: string
- Outcomes: string
- TotalAttendance: int
- TotalBenefic: int

#### Notification
- NotificationId: int
- RecipientMemberId: int
- Type: string
- Title: string
- Message: string
- ReportId: int?
- StateReportId: int?
- CycleId: int?
- IsRead: bool

#### ReportActivityLog
- LogId: int
- ReportId: int
- ActionBy: int
- Action: string
- Notes: string
- ActionAt: datetime

---

## 7. State Diagrams

### 7.1 Unit Report Status State Machine

```
                    ┌──────────────────────┐
                    │   [CREATED]          │
                    │   (by any unit       │
                    │    exec officer)     │
                    └──────────────┬───────┘
                                   │
                                   ▼
              ┌──────────────────────────────────────────┐
          ┌──►│     DRAFT                                │◄─┐
          │   │   Dept sections                          │  │
          │   │   being filled.                          │  │ [Officer
          │   │   Auto-saved to DB                       │  │  edits
          │   │   on every change]                       │  │  section]
          │   │                                          │  │
          │   └──────────────┬───────────────────────────┘  │
          │                  │                              │
          │                  │ [All 9 dept sections         │
          │                  │  marked Submitted]           │
          │                  ▼                              │
          │   ┌──────────────────────────────────────┐     │
          │   │  SUBMITTED TO                         │     │
          │   │  PRESIDENT                            │     │
          │   │                                       │     │
          │   │ Unit President sees                   │     │
          │   │ full report + all                     │     │
          │   │ dept sections.                        │────┐│
          │   │ Can add note.                         │    ││
          │   └──────────────┬────────────────────────┘    ││
          │                  │                            ││
          │                  │ [President approves]       ││
          │                  ▼                            ││
          │   ┌──────────────────────────────────────┐    ││
          │   │  APPROVED BY                          │    ││
          │   │  PRESIDENT                            │    ││
          │   │                                       │    ││
          │   │ Automatically                         │    ││
          │   │ forwarded to State GS                 │    ││
          │   └──────────────┬────────────────────────┘    ││
          │                  │                            ││
          │                  │ [Auto-forwarded on approval]
          │                  ▼                            ││
          │   ┌──────────────────────────────────────┐    ││
          │   │  SUBMITTED TO STATE                   │    ││
          │   │                                       │◄───┘│
          │   │ State GS reviews.                     │     │
          │   │ Can see all units                     │[State GS
          │   │ in their state.                       │ sends back]
          │   │ Can add state note.                   │
          │   └──────────────┬────────────────────────┘
          │                  │
          │                  │ [State GS approves]
          │                  ▼
          │   ┌──────────────────────────────────────┐
          │   │  APPROVED BY STATE                    │
          │   │                                       │
          │   │ Forwarded to                          │
          │   │ National GS queue                     │
          │   └──────────────┬────────────────────────┘
          │                  │
          │                  │ [Auto-forwarded on approval]
          │                  ▼
          │   ┌──────────────────────────────────────┐
          │   │ SUBMITTED TO NATIONAL                 │
          │   │                                       │
          │   │ National GS sees full                 │
          │   │ national queue.                       │
          │   └──────────────┬────────────────────────┘
          │                  │
          │                  │ [National GS acknowledges]
          │                  ▼
          │   ┌──────────────────────────────────────┐
          └───┤     ACKNOWLEDGED                      │
              │                                       │
              │ Terminal state.                       │
              │ Report is complete.                   │
              │ Counts in analytics.                  │
              └──────────────────────────────────────┘

ADDITIONAL TRANSITIONS:
────────────────────────────────────────────────────────────────
[Cycle locked after deadline]  → All DRAFT reports are
                                  flagged as OVERDUE
[Officer edits after deadline] → Blocked. Read-only.
[Dept section resubmitted]     → Updates DepartmentReport.
                                  Resets to Draft if
                                  President had approved.
```

### 7.2 State Report Status Machine

```
                    ┌──────────────────────────────┐
                    │   [State GS starts report]    │
                    └──────────────────┬────────────┘
                                       │
                                       ▼
            ┌──────────────────────────────────┐
        ┌───►│     DRAFT                        │
        │    │   State GS fills                 │
        │    │   5 sections.                    │
        │    │   Can add multiple               │
        │ [State GS   programs under Q3.  │  [State GS continues
        │ continues]  └────────────┬──────┘     editing]
        │            │
        │            │ [State GS submits]
        │            ▼
        │  ┌──────────────────────────────────┐
        │  │  SUBMITTED TO NATIONAL            │
        │  │                                   │
        │  │ National GS reviews.              │
        │  │ Can add national note.            │◄───────────┐
        │  │                                   │            │
        │  │ [National sends back]             │
        │  └──────────────┬──────────────────┘
        │                 │
        │                 │ [National GS acknowledges]
        │                 ▼
        │  ┌──────────────────────────────────┐
        └──┤      ACKNOWLEDGED                 │
           │                                   │
           │ Terminal state.                   │
           │ Data feeds national               │
           │ analytics dashboard.              │
           └──────────────────────────────────┘
```

### 7.3 Department Report Section State

```
[Officer opens their section]
       │
       ▼
  ┌─────────────────┐
┌─►│    DRAFT        │
│  │  Auto-saved     │
│  │  every field    │
│  │  change.        │
│  │  Officer can    │
│  │  edit freely.   │
│  └────────┬────────┘
│           │
│           │ [Officer clicks Submit Section]
│           ▼
│  ┌─────────────────┐
│  │  SUBMITTED      │
│  │                 │
│  │  Read-only      │
│  │  unless         │
├──┤◄─[President sends back]─DRAFT
│  │  President      │
│  │  rejects.       │
│  └─────────────────┘
│
└──[Officer recalls before President reviews]
```

---

## 8. Data Flow Diagrams

### 8.1 DFD Level 0 – Context Diagram

```
    ┌──────────────────┐    ┌──────────────────┐
    │  Department      │    │  Unit President  │
    │  Officer         │    │                  │
    │  (Unit Level)    │    │                  │
    └────────┬─────────┘    └────────┬─────────┘
     submit dept section    approve/forward
             │                       │
             └───────┐───────────────┘
                     │
            ┌────────▼──────────┐
            │                   │
            │ AMSA REPORTING    │
            │     SYSTEM        │
            │                   │
            └────────┬──────────┘
                     │
    ┌────────────────┴────────────────┐
    │                                  │
    ▼                                  ▼
┌──────────────────┐    ┌──────────────────────────┐
│  State GS /      │    │  National GS             │
│  State President ├───►│  All reports             │
│                  │    │                          │
└─────────┬────────┘    └────────┬─────────────────┘
state rpt │                      │ acknowledge
          │                      ▼
          │            ┌──────────────────┐
          │            │ National Pres /  │
          └───────────►│ VP               │
                       │                  │
                       └──────────────────┘
                    analytics/dashboard

External Systems:
┌──────────────────┐
│   AMSA API       │◄─── Auth tokens, member data,
│  (AmsaDB)        │      unit/state/org lookups
└──────────────────┘
```

### 8.2 DFD Level 1 – Core Processes

```
        ┌──────────────────────────────────────────┐
        │   AMSA REPORTING SYSTEM                  │
        │                                          │
        │  ┌────────────────────┐  ┌────────────┐ │
        │  │  P1: AUTHENTICATE   │  │ P2: MANAGE │ │
        │  │                     │  │ CYCLES     │ │
        │  │ - Receive MKANID   │  │            │ │
        │  │   + credential     │  │ - Create   │ │
        │  │ - Call AMSA API    │  │   monthly  │ │
        │  │ - Store JWT +      │  │   window   │ │
        │  │   role claims      │  │ - Set      │ │
        │  └────────┬────────────┘  │   deadline │ │
        │           │               │ - Lock on  │ │
        │      JWT + roles       │   deadline  │ │
        │           │               │ - Trigger  │ │
        │           ▼               │   reminders│ │
        │  ┌────────────────────┐   └────┬───────┘ │
        │  │  P3: SUBMIT        │        │         │
        │  │  DEPT SECTION      │  Cycle data    │
        │  │                    │        │         │
        │  │ - Validate form   │◄───────┘         │
        │  │ - Run compliance  │                  │
        │  │   engine          │                  │
        │  │ - Auto-save draft │                  │
        │  │ - Mark submitted  │                  │
        │  └────────┬───────────┘                  │
        │           │                             │
        │      Report data                        │
        │           │                             │
        │           ▼                             │
        │  ┌────────────────────┐  ┌────────────┐ │
        │  │  P4: ROUTE REPORT  │  │ P5:        │ │
        │  │                    │  │ COMPLIANCE │ │
        │  │ - Enforce state   │  │ ENGINE     │ │
        │  │   machine trans.  │  │            │ │
        │  │ - Notify next    │──►│ - Check    │ │
        │  │   actor          │  │   thresholds
        │  │ - Write activity │  │ - Set      │ │
        │  │   log            │  │   flags    │ │
        │  │ - Block if cycle │  │ - Surface  │ │
        │  │   locked         │  │   flags    │ │
        │  └──────────────────┘   └────┬───────┘ │
        │                              │         │
        │                    ┌─────────┴───────┐ │
        │                    │                 │ │
        │                    ▼                 ▼ │
        │          ┌────────────────────┐  ┌──────────┐
        │          │  P6: ANALYTICS &   │  │ P7:      │
        │          │  DASHBOARD         │  │ REMINDER │
        │          │                    │  │ SERVICE  │
        │          │ - Aggregate by     │  │          │
        │          │   month            │  │ - Run    │
        │          │ - Compliance       │  │   schedule
        │          │   summary          │  │ - 7-day  │
        │          │ - Submission rates │  │   warning
        │          │ - Dept performance │  │ - 3-day  │
        │          │ - State comparison │  │   warning
        │          │ - Escalation       │  │ - 1-day  │
        │          │   alerts           │  │   warning
        │          └───────────────────┘   └────┬──────┘
        │                                       │
        │          ┌──────────────────────────┐ │
        │          │  P8: NOTIFICATIONS       │ │
        │          │                          │ │
        │          │ - In-app alerts          │ │
        │          │ - Status changes         │ │
        │          │ - Approval actions       │ │
        │          │ - Deadline warnings      │ │
        │          │ - Cycle lock alerts      │ │
        │          └──────────────────────────┘ │
        │                                          │
        └──────────────────────────────────────────┘

Data Stores:
┌──────────────────────────────────────────────────────────┐
│  D1: AmsaReportingDB  │ D2: AmsaDB (via AMSA API – read) │
└──────────────────────────────────────────────────────────┘
```

### 8.3 DFD Level 2 – Report Submission Process (P3 Drill-Down)

```
Officer                 System                    DB
─────                   ──────                    ──
Opens form           → P3.1: Load draft
                       check cycle locked?
                       if locked → READ ONLY
                     → Fetch existing data    → DepartmentReport
                     ← Render form with saved values

Fills fields         → P3.2: Auto-save draft
                       debounce 2 seconds    → UPDATE DepartmentReport
                     ← "Saved" indicator

Submits section      → P3.3: Validate required fields
                       all required = filled?
                       NO  → return field errors
                       YES → continue

                     → P3.4: Run ComplianceEngine
                       TaleemReport?
                         sessions >= 4?     → IsSessionsCompliant
                         attendance >= 75%? → IsAttendanceCompliant
                       WelfareReport?
                         programs >= 2?     → IsCompliant
                       TablighReport?
                         has on-campus?     → IsCompliant
                       SportReport?
                         members >= 75%?    → IsAttendanceCompliant
                       FinanceReport?
                         collected >= expected? → IsFullyCollected
                         compute CollectionRate

                     → P3.5: Save + mark Submitted
                       UPDATE DepartmentReport.Status = 'Submitted'
                       SET SubmittedAt = NOW()
                       INSERT ReportActivityLog       ────────→ DB

                     → P3.6: Check if all 9 sections submitted
                       all submitted?
                       YES → UPDATE Report.Status =
                             'SubmittedToPresident'
                             Notify Unit President
                       NO  → remain in Draft

                     ← Confirmation screen
```

---

## 9. Feature Requirements

### 9.1 Authentication & Authorization

The reporting system authenticates users via the AMSA API token endpoint. On login, the user enters their MKAN ID and a credential. The system calls `POST /api/auth/token` and receives a JWT containing role claims that determine what the user can see and do.

| Role Claim | Can Submit | Can Approve | Can See |
|---|---|---|---|
| `Taleem:unit` | Taleem section only | – | Own section |
| `Tabligh:unit` | Tabligh section only | – | Own section |
| `FinancialSecretary:unit` | Finance section only | – | Own section |
| `President:unit` | All sections (if needed) | Unit reports | Full unit report |
| `GeneralSecretary:state` | – | State report | State report + All units in state |
| `President:state` | – | State report | State report + All units in state |
| `GeneralSecretary:national` | – | – | All reports + Full national view |
| `President:national` | – | – | Full national + analytics |

### 9.2 Reporting Cycles

National GS creates a `ReportingCycle` by specifying the `ReportMonth` (YYYY-MM) and `SubmissionDeadline`. Only one cycle can exist per month – the unique constraint on `ReportMonth` enforces this. A background service checks daily: if the deadline has passed, `IsLocked` is set to `true`. Locked cycles prevent new submissions. Existing drafts become read-only. National GS can view all cycles with their submission status breakdown.

### 9.3 Unit Report – Department Sections

Each of the 9 department sections has a dedicated Blazor form component bound to its EF Core entity. Fields map exactly to the AMSA reporting format document.

| Department | Key Fields | Compliance Threshold |
|---|---|---|
| **Taleem** | Avg attendance, total members, session duration, test count, sessions organised, notes | Sessions >= 4 AND attendance >= 75% |
| **Tabligh** | Activities detail, purpose, attendance count, beneficiaries, on-campus flag | At least 1 on-campus activity |
| **Welfare** | Program count, participant count, activities with dates, brief report | Programs >= 2 |
| **Sport** | Games played, member participants, non-member participants, total members | Member attendance >= 75% |
| **Finance** | Dues collected (decimal), expected dues (manual input), defaulters reason | Flag if collected < expected |
| **Health** | Activities with dates, beneficiary count, notes | None (descriptive only) |
| **Secondary School** | Attended Jama'at meeting (bool), parent support efforts, jambite participation | None (descriptive only) |
| **Tajneed** | Has accurate tajneed (bool), improvement efforts, accuracy plans | None (descriptive only) |
| **General** | Challenges faced, other activities, notes | None (catch-all) |

### 9.4 Compliance Engine

The `ComplianceEngine` runs on every section submission and sets boolean compliance flags on the stored entities. It does not block submission – it flags and surfaces issues to the Unit President and State GS for review.

```csharp
public class ComplianceEngine
{
    public void Evaluate(TaleemReport r)
    {
        r.IsSessionsCompliant = r.SessionsOrganized >= 4;
        r.IsAttendanceCompliant = r.TotalMemberCount > 0 &&
            (double)r.AverageAttendanceCount / r.TotalMemberCount * 100 >= 75;
    }

    public void Evaluate(TablighReport r)
    {
        r.IsCompliant = r.HasOnCampusActivity;
    }

    public void Evaluate(WelfareReport r)
    {
        r.IsCompliant = r.ProgramCount >= 2;
    }

    public void Evaluate(SportReport r)
    {
        r.IsAttendanceCompliant = r.TotalMemberCount > 0 &&
            (double)r.MemberParticipantCount / r.TotalMemberCount * 100 >= 75;
    }

    public void Evaluate(FinanceReport r)
    {
        r.CollectionRate = r.ExpectedDuesAmount > 0
            ? (r.DuesCollected / r.ExpectedDuesAmount) * 100 : 0;
        r.IsFullyCollected = r.DuesCollected >= r.ExpectedDuesAmount;
    }
}
```

### 9.5 Report Workflow – Routing & Approval

When all 9 department sections are marked `Submitted`, the Report status automatically moves to `SubmittedToPresident`. The Unit President receives an in-app notification. President can view the full report, see compliance flags, add a `PresidentialNote`, then approve. On approval, status moves to `ApprovedByPresident` and immediately to `SubmittedToState` (auto-forwarded). State GS sees all unit reports for their state with submission status at a glance. State GS can add a `StateNote` and approve. Report moves to `ApprovedByState` then `SubmittedToNational`. National GS sees the full national queue. Acknowledges reports. They are then terminal. Every status change is logged in `ReportActivityLog` with `MemberId`, action string, and timestamp.

### 9.6 State Report

State GS fills a 5-section form once per cycle:

- **Q1: Unit president attendance** – two integer inputs (attended, total). System auto-formats as 'X out of Y'
- **Q3: Programs** – dynamic form where GS can add multiple programs, each with name, objectives, outcomes, attendance, beneficiaries
- **Q4: Has a dedicated NationalSupportNeeded field** – National GS can filter state reports where this is non-empty as an escalation queue

On submit, status moves to `SubmittedToNational`. National GS acknowledges to close the loop.

### 9.7 Reminder Service

| Trigger | Recipients | Message |
|---|---|---|
| 7 days before deadline | All unit exec officers with pending sections | Monthly report for [Month] is due in 7 days. Please submit your section. |
| 3 days before deadline | Unit Presidents with unapproved reports | 3 days left. [N] sections pending in your unit. |
| 1 day before deadline | State GS with unapproved state reports | Final reminder: State report for [Month] due tomorrow. |
| Deadline day (midnight) | National GS | Cycle [Month] locked. [N] units submitted. [N] overdue. |
| When report submitted | Unit President | [Unit] has submitted all department sections for [Month]. |
| When approved | State GS | [Unit] report approved by president. Awaiting your review. |

### 9.8 Analytics Dashboard (National)

- Submission rate per cycle: % of units that submitted on time
- Compliance summary: number of units that hit Taleem, Welfare, Tabligh, Sport thresholds
- Finance collection aggregate: total dues collected nationally per month, trend chart
- Department activity counts: total Tabligh activities, total Welfare programs, total health beneficiaries – rolled up across all units
- State-level comparison: which states have highest/lowest unit compliance rates
- Escalations view: state reports with non-empty NationalSupportNeeded field
- Historical trend: submission rates, compliance rates, finance collection over the last 12 months

---

## 10. UI/UX Requirements

### 10.1 Page Structure

| Page / Component | Role Access | Purpose |
|---|---|---|
| Login (`/login`) | All | MKAN ID + credential → AMSA API token |
| My Dashboard (`/dashboard`) | All | Role-appropriate summary + pending actions |
| My Section (`/report/{month}/{dept}`) | Dept Officers | Fill and submit department section |
| Unit Report (`/unit/{unitId}/{month}`) | Unit President | View all sections, compliance flags, approve |
| State Dashboard (`/state/{stateId}/{month}`) | State GS / Pres | All unit statuses + state report form |
| National Dashboard (`/national/{month}`) | National GS / Pres / VP | Full national view, analytics, escalations |
| Cycle Management (`/cycles`) | National GS | Create, view, lock reporting cycles |
| Notifications (`/notifications`) | All | In-app notification inbox |
| Report History (`/history`) | All (scoped by role) | Past reports, searchable by month/unit/state |

### 10.2 Key UX Principles

- **Auto-save:** Every field change in a department section triggers a debounced save (2 seconds) to the DB. Officers never lose work.
- **Section progress:** Unit President dashboard shows a visual indicator for each of the 9 departments (not started / in progress / submitted / compliant / non-compliant)
- **Read-only after lock:** When cycle is locked, all forms become read-only. No confusing error messages – the UI just shows the locked state clearly
- **Compliance badges:** Non-compliant sections show a clear visual badge (e.g. amber warning). Compliant sections show green. This is visible to the president before they approve
- **Mobile-first:** Officers are likely filling reports on their phones. All forms must work well on small screens
- **Network resilience:** Given Nigerian network conditions, the Blazor Server SignalR connection drop is handled gracefully – show a 'reconnecting' banner, not a blank page

---

## 11. Non-Functional Requirements

| Category | Requirement |
|---|---|
| **Performance** | Dashboard loads in < 2 seconds. Report form auto-save completes in < 500ms. |
| **Availability** | 99% uptime during the last 7 days of each month (peak submission period). |
| **Scalability** | Must handle all AMSA units across Nigeria submitting concurrently (estimated ~50-200 concurrent users at peak). |
| **Security** | All endpoints require valid JWT. Role claims validated server-side on every Blazor circuit action. No cross-unit data leakage. |
| **Data integrity** | Report state machine transitions are atomic – partial state changes roll back on failure. |
| **Audit** | Every status change, approval, and rejection is logged in ReportActivityLog with actor MemberId and timestamp. Immutable log. |
| **Connectivity** | Auto-save to DB on every field change so no data is lost on connection drop. Blazor SignalR reconnection handled gracefully. |
| **Browser support** | Chrome, Firefox, Safari mobile. No IE requirement. |
| **Data retention** | All historical report data retained indefinitely in AmsaReportingDB. No purge policy. |

---

## 12. Phased Delivery Plan

### Phase 1 – Core Submission Flow (MVP)

- AmsaReportingDB schema created and EF Core migrations applied
- AMSA API token-based login working in Blazor
- Reporting cycle creation by National GS
- All 9 department section forms for unit officers with auto-save
- Report status state machine: Draft → SubmittedToPresident → ApprovedByPresident → SubmittedToState → ApprovedByState → SubmittedToNational → Acknowledged
- Activity log for every transition
- Basic unit president approval screen

### Phase 2 – Compliance + State Reports

- ComplianceEngine running on all 9 section submissions
- Compliance badges in Unit President view
- State GS dashboard with all unit submission statuses
- State report form (5 sections + dynamic programs list)
- National GS acknowledgement queue
- In-app notifications for all status changes

### Phase 3 – Reminders + Analytics

- Background reminder service (7-day, 3-day, 1-day, deadline-day triggers)
- Cycle lock on deadline
- National analytics dashboard: submission rates, compliance summary, finance aggregates
- State comparison views
- Escalations view (NationalSupportNeeded queue)
- Report history with search by month, unit, state

### Phase 4 – Export & Polish

- Export monthly report summary as PDF
- Export finance collection data as Excel
- Email reminder integration (SendGrid)
- WhatsApp notification integration (Twilio) – since that is the current workflow channel
- Historical trend charts (12-month rolling windows)

---

## 13. AMSA API Integration

### 13.1 App Registration

```json
POST /api/auth/apps
{
  "appId":   "amsa-reporting",
  "appName": "AMSA Reporting System",
  "allowedScopes": [
    "read:members",
    "read:organization",
    "read:statistics",
    "read:exco"
  ],
  "tokenExpirationHours": 8
}
```

### 13.2 User Login Flow

```
User enters MKAN ID + password on Blazor login page
         │
         ▼
POST /api/auth/token {
  "appId": "amsa-reporting",
  "mkanId": 12345,
  "requestedScopes": ["read:members","read:organization","read:statistics"]
}
         │
         ▼
JWT returned with claims:
{
  "sub":       "100",
  "mkanId":    "12345",
  "firstName": "Ibrahim",
  "unitId":    "5",
  "scope":     "read:members",
  "scope":     "read:organization",
  "role":      "President:unit",      ← drives what user sees
  "role":      "Taleem:unit"
}
         │
         ▼
Blazor stores JWT in protected server-side session.
Role claims are parsed into a ClaimsPrincipal.
[Authorize] and role checks run on every Blazor circuit action
– not just initial page load.
```

### 13.3 Cross-DB Reference Strategy

`UnitId`, `StateId`, and `MemberId` values stored in `AmsaReportingDB` are integers that reference `AmsaDB`. There are no cross-database foreign key constraints at the SQL Server level. Instead:

- Before creating a Report, the service calls the AMSA API to validate the UnitId exists and is active
- Before accepting a submission, the service confirms the SubmittedBy MemberId is a valid member of that unit
- Unit and State names are fetched from the AMSA API at display time (not stored in ReportingDB) – this ensures they always reflect the current org structure
- The AMSA API hierarchy endpoint provides the National → State → Unit tree for navigation in the Blazor UI

---

## 14. Open Questions & Decisions

| # | Question | Options | Recommended |
|---|---|---|---|
| 1 | Can a Unit President submit a department section on behalf of an officer who is absent? | A) Yes, President can fill any section<br/>B) No, must be the assigned officer | A – flexibility needed for small units |
| 2 | What happens if a state has a unit that doesn't have a particular department active? | A) Section is required but can be marked 'N/A'<br/>B) Sections can be disabled per unit | A for MVP simplicity |
| 3 | Should the Female Wing have separate reporting from the Male Wing? | A) Separate reports<br/>B) Combined under one Unit report | Pending – constitution says unit level is joint under one president |
| 4 | Historical data import – how far back? | A) Start fresh from go-live<br/>B) Import past months from Word docs | A for MVP, B as optional Phase 4 feature |
| 5 | Who can create a ReportingCycle? | A) National GS only<br/>B) National GS + VP + President | A – single point of control |

---

## 15. Glossary

| Term | Definition |
|---|---|
| **AMSA** | Ahmadiyya Muslim Students' Association Nigeria |
| **AHIC** | Annual Higher Institution Convention – the annual AGM + convention of AMSA |
| **MKAN ID** | Unique membership identifier issued by the Ahmadiyya Muslim Jama'at Nigeria |
| **Tajneed** | Membership register/census – maintaining an accurate count and list of members |
| **Taleem** | Islamic education/learning sessions – a core weekly activity at unit level |
| **Tabligh** | Preaching/propagation activities – the evangelism arm of AMSA |
| **Jambite** | Newly admitted university students (JAMB = Joint Admissions and Matriculation Board) |
| **Reporting Cycle** | A monthly reporting window created by the National GS with a submission deadline |
| **Compliance Engine** | Service that evaluates department report fields against constitutional minimum standards |
| **ReportingDB** | AmsaReportingDB – the SQL Server database owned by the Reporting System |
| **AmsaDB** | The existing SQL Server database managed by the AMSA API |
| **JWT** | JSON Web Token – the auth token issued by the AMSA API containing scopes and role claims |
| **Role claim** | A claim in the JWT of format 'Department:Level' e.g. 'President:unit' that drives authorization |

---

**– End of Document –**  
**AMSA Nigeria Reporting System PRD v1.0.0 | March 2026**
