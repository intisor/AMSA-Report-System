-- ============================================================
-- AMSA REPORTING SYSTEM - DATABASE SCHEMA
-- Database: AmsaReportingDB
-- SQL Server | Companion to AmsaDB
-- ============================================================

-- Run this on the same SQL Server instance as AmsaDB
-- CREATE DATABASE AmsaReportingDB;
-- GO
-- USE AmsaReportingDB;
-- GO

-- ============================================================
-- SECTION 1: REPORTING CYCLES
-- National GS creates a cycle for each month.
-- Controls deadlines, reminders, and locks.
-- ============================================================

CREATE TABLE ReportingCycles (
    CycleId         INT IDENTITY(1,1) PRIMARY KEY,
    ReportMonth     CHAR(7)       NOT NULL,  -- Format: 'YYYY-MM' e.g. '2025-01'
    SubmissionDeadline  DATETIME2 NOT NULL,
    ReminderSentAt  DATETIME2     NULL,      -- Tracks when last reminder was sent
    IsLocked        BIT           NOT NULL DEFAULT 0, -- Locked after deadline
    CreatedBy       INT           NOT NULL,  -- MemberId from AmsaDB
    CreatedAt       DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2     NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT UQ_ReportingCycles_Month UNIQUE (ReportMonth)
);
GO

-- ============================================================
-- SECTION 2: UNIT REPORTS
-- One Report per Unit per ReportingCycle.
-- Tracks the overall submission status through the chain.
-- ============================================================

CREATE TABLE Reports (
    ReportId        INT IDENTITY(1,1) PRIMARY KEY,
    CycleId         INT           NOT NULL,
    UnitId          INT           NOT NULL,  -- FK → AmsaDB.dbo.Units
    SubmittedBy     INT           NOT NULL,  -- MemberId who started the report
    Status          NVARCHAR(50)  NOT NULL DEFAULT 'Draft',
    -- Status values:
    -- 'Draft'                  → report created, depts filling sections
    -- 'SubmittedToPresident'   → all dept sections submitted
    -- 'ApprovedByPresident'    → president reviewed and approved
    -- 'SubmittedToState'       → forwarded to State GS
    -- 'ApprovedByState'        → State GS reviewed
    -- 'SubmittedToNational'    → forwarded to National GS
    -- 'Acknowledged'           → National GS acknowledged

    PresidentialNote    NVARCHAR(MAX) NULL,  -- Unit President's comments
    StateNote           NVARCHAR(MAX) NULL,  -- State GS's comments
    NationalNote        NVARCHAR(MAX) NULL,  -- National GS's comments

    PresidentApprovedBy     INT       NULL,  -- MemberId of approving president
    PresidentApprovedAt     DATETIME2 NULL,
    StateApprovedBy         INT       NULL,
    StateApprovedAt         DATETIME2 NULL,
    NationalAcknowledgedBy  INT       NULL,
    NationalAcknowledgedAt  DATETIME2 NULL,

    CreatedAt       DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2     NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_Reports_Cycles    FOREIGN KEY (CycleId) REFERENCES ReportingCycles(CycleId),
    CONSTRAINT UQ_Reports_Unit_Cycle UNIQUE (UnitId, CycleId), -- One report per unit per month
    CONSTRAINT CHK_Reports_Status CHECK (Status IN (
        'Draft', 'SubmittedToPresident', 'ApprovedByPresident',
        'SubmittedToState', 'ApprovedByState', 
        'SubmittedToNational', 'Acknowledged'
    ))
);
GO

-- ============================================================
-- SECTION 3: DEPARTMENT REPORTS
-- One row per department per Report.
-- Each department officer submits their own section.
-- ============================================================

CREATE TABLE DepartmentReports (
    DepartmentReportId  INT IDENTITY(1,1) PRIMARY KEY,
    ReportId            INT           NOT NULL,
    Department          NVARCHAR(50)  NOT NULL,
    -- Department values:
    -- 'Taleem' | 'Tabligh' | 'Welfare' | 'Sport' | 'Finance'
    -- 'Health' | 'SecondarySchool' | 'Tajneed' | 'General'

    SubmittedBy         INT           NULL,  -- MemberId of dept officer
    Status              NVARCHAR(20)  NOT NULL DEFAULT 'Draft',
    -- 'Draft' | 'Submitted'

    SubmittedAt         DATETIME2     NULL,
    CreatedAt           DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2     NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_DeptReports_Reports FOREIGN KEY (ReportId) REFERENCES Reports(ReportId),
    CONSTRAINT UQ_DeptReports_Report_Dept UNIQUE (ReportId, Department),
    CONSTRAINT CHK_DeptReports_Department CHECK (Department IN (
        'Taleem', 'Tabligh', 'Welfare', 'Sport', 'Finance',
        'Health', 'SecondarySchool', 'Tajneed', 'General'
    )),
    CONSTRAINT CHK_DeptReports_Status CHECK (Status IN ('Draft', 'Submitted'))
);
GO

-- ============================================================
-- SECTION 4: TALEEM (EDUCATION) DEPARTMENT REPORT
-- Minimum standard: 4 sessions/month, 75% attendance,
-- at least 1 session in mosque (captured in notes)
-- ============================================================

CREATE TABLE TaleemReports (
    TaleemReportId          INT IDENTITY(1,1) PRIMARY KEY,
    DepartmentReportId      INT           NOT NULL UNIQUE,

    -- Q1: Average attendance
    AverageAttendanceCount  INT           NOT NULL,
    TotalMemberCount        INT           NOT NULL,  -- for computing percentage
    AttendanceBelowThresholdReason NVARCHAR(MAX) NULL, -- required if below 75%

    -- Q2: Session duration
    SessionDurationMinutes  INT           NOT NULL,

    -- Q3: Monthly test
    MonthlyTestCount        INT           NOT NULL,

    -- Q4: Sessions organized
    SessionsOrganized       INT           NOT NULL,

    -- Notes field (captures mosque session info + anything extra)
    Notes                   NVARCHAR(MAX) NULL,

    -- Computed compliance flags (set by application on submission)
    IsSessionsCompliant     BIT           NOT NULL DEFAULT 0,  -- sessions >= 4
    IsAttendanceCompliant   BIT           NOT NULL DEFAULT 0,  -- attendance >= 75%

    CONSTRAINT FK_TaleemReports_DeptReports 
        FOREIGN KEY (DepartmentReportId) REFERENCES DepartmentReports(DepartmentReportId)
);
GO

-- ============================================================
-- SECTION 5: TABLIGH (PREACHING) DEPARTMENT REPORT
-- Minimum standard: at least 1 on-campus tabligh exercise
-- ============================================================

CREATE TABLE TablighReports (
    TablighReportId         INT IDENTITY(1,1) PRIMARY KEY,
    DepartmentReportId      INT           NOT NULL UNIQUE,

    -- Q1: Activities carried out
    ActivitiesDetail        NVARCHAR(MAX) NOT NULL,

    -- Q2: Purpose, attendance, beneficiaries
    Purpose                 NVARCHAR(MAX) NOT NULL,
    AttendanceCount         INT           NOT NULL,
    Beneficiaries           NVARCHAR(MAX) NOT NULL,

    -- Notes
    Notes                   NVARCHAR(MAX) NULL,

    -- Compliance: at least 1 on-campus activity
    HasOnCampusActivity     BIT           NOT NULL DEFAULT 0,
    IsCompliant             BIT           NOT NULL DEFAULT 0,

    CONSTRAINT FK_TablighReports_DeptReports 
        FOREIGN KEY (DepartmentReportId) REFERENCES DepartmentReports(DepartmentReportId)
);
GO

-- ============================================================
-- SECTION 6: WELFARE DEPARTMENT REPORT
-- Minimum standard: 2 activities, 1 must be for members
-- ============================================================

CREATE TABLE WelfareReports (
    WelfareReportId         INT IDENTITY(1,1) PRIMARY KEY,
    DepartmentReportId      INT           NOT NULL UNIQUE,

    -- Q1: How many programs
    ProgramCount            INT           NOT NULL,

    -- Q2: Participants
    ParticipantCount        INT           NOT NULL,

    -- Q3: Activities with dates
    ActivitiesWithDates     NVARCHAR(MAX) NOT NULL,

    -- Q4: Brief report with beneficiaries
    BriefReport             NVARCHAR(MAX) NOT NULL,

    -- Notes
    Notes                   NVARCHAR(MAX) NULL,

    -- Compliance: >= 2 programs
    IsCompliant             BIT           NOT NULL DEFAULT 0,

    CONSTRAINT FK_WelfareReports_DeptReports 
        FOREIGN KEY (DepartmentReportId) REFERENCES DepartmentReports(DepartmentReportId)
);
GO

-- ============================================================
-- SECTION 7: SPORT DEPARTMENT REPORT
-- Minimum standard: 2 activities, 75% member attendance
-- ============================================================

CREATE TABLE SportReports (
    SportReportId           INT IDENTITY(1,1) PRIMARY KEY,
    DepartmentReportId      INT           NOT NULL UNIQUE,

    -- Q1: Games/sports played
    GamesPlayed             NVARCHAR(MAX) NOT NULL,

    -- Q2: Participation numbers
    MemberParticipantCount      INT       NOT NULL,
    NonMemberParticipantCount   INT       NOT NULL,
    TotalMemberCount            INT       NOT NULL,  -- for computing 75%

    -- Notes
    Notes                   NVARCHAR(MAX) NULL,

    -- Compliance: attendance >= 75%
    IsAttendanceCompliant   BIT           NOT NULL DEFAULT 0,

    CONSTRAINT FK_SportReports_DeptReports 
        FOREIGN KEY (DepartmentReportId) REFERENCES DepartmentReports(DepartmentReportId)
);
GO

-- ============================================================
-- SECTION 8: FINANCE DEPARTMENT REPORT
-- Every member must be regular in payment
-- ============================================================

CREATE TABLE FinanceReports (
    FinanceReportId         INT IDENTITY(1,1) PRIMARY KEY,
    DepartmentReportId      INT           NOT NULL UNIQUE,

    -- Q1: Dues collected
    DuesCollected           DECIMAL(10,2) NOT NULL,

    -- Officer inputs expected dues manually
    ExpectedDuesAmount      DECIMAL(10,2) NOT NULL,

    -- Q2: Why others didn't pay
    DefaultersReason        NVARCHAR(MAX) NULL,  -- Required if DuesCollected < ExpectedDues

    -- Notes
    Notes                   NVARCHAR(MAX) NULL,

    -- Compliance: full collection vs partial
    CollectionRate          DECIMAL(5,2)  NULL,  -- computed: (DuesCollected/ExpectedDues)*100
    IsFullyCollected        BIT           NOT NULL DEFAULT 0,

    CONSTRAINT FK_FinanceReports_DeptReports 
        FOREIGN KEY (DepartmentReportId) REFERENCES DepartmentReports(DepartmentReportId)
);
GO

-- ============================================================
-- SECTION 9: HEALTH DEPARTMENT REPORT
-- Activities must target student community
-- ============================================================

CREATE TABLE HealthReports (
    HealthReportId          INT IDENTITY(1,1) PRIMARY KEY,
    DepartmentReportId      INT           NOT NULL UNIQUE,

    -- Q1: Activity with date
    ActivitiesWithDates     NVARCHAR(MAX) NOT NULL,

    -- Q2: Beneficiaries
    BeneficiaryCount        INT           NOT NULL,

    -- Notes
    Notes                   NVARCHAR(MAX) NULL,

    CONSTRAINT FK_HealthReports_DeptReports 
        FOREIGN KEY (DepartmentReportId) REFERENCES DepartmentReports(DepartmentReportId)
);
GO

-- ============================================================
-- SECTION 10: SECONDARY SCHOOL DEPARTMENT REPORT
-- At least 1 interaction session per month
-- ============================================================

CREATE TABLE SecondarySchoolReports (
    SecondarySchoolReportId INT IDENTITY(1,1) PRIMARY KEY,
    DepartmentReportId      INT           NOT NULL UNIQUE,

    -- Q1: Jama'at and circuit meeting attendance
    AttendedJamaatMeeting   BIT           NOT NULL,
    ParentSupportEfforts    NVARCHAR(MAX) NULL,  -- Required if AttendedJamaatMeeting = true

    -- Q2: Jambite/secondary school participation
    JambiteParticipation    NVARCHAR(MAX) NOT NULL,

    -- Notes
    Notes                   NVARCHAR(MAX) NULL,

    CONSTRAINT FK_SecSchoolReports_DeptReports 
        FOREIGN KEY (DepartmentReportId) REFERENCES DepartmentReports(DepartmentReportId)
);
GO

-- ============================================================
-- SECTION 11: TAJNEED (MEMBER REGISTRY) REPORT
-- AGS responsibility per 2016 amendment
-- ============================================================

CREATE TABLE TajneedReports (
    TajneedReportId         INT IDENTITY(1,1) PRIMARY KEY,
    DepartmentReportId      INT           NOT NULL UNIQUE,

    -- Q1: Accurate tajneed
    HasAccurateTajneed      BIT           NOT NULL,

    -- Q2: Improvement efforts
    ImprovementEfforts      NVARCHAR(MAX) NOT NULL,

    -- Q3: Accuracy plans
    AccuracyPlans           NVARCHAR(MAX) NOT NULL,

    -- Notes
    Notes                   NVARCHAR(MAX) NULL,

    CONSTRAINT FK_TajneedReports_DeptReports 
        FOREIGN KEY (DepartmentReportId) REFERENCES DepartmentReports(DepartmentReportId)
);
GO

-- ============================================================
-- SECTION 12: GENERAL / ADDITIONAL REPORT
-- Challenges and any other activities
-- ============================================================

CREATE TABLE GeneralReports (
    GeneralReportId         INT IDENTITY(1,1) PRIMARY KEY,
    DepartmentReportId      INT           NOT NULL UNIQUE,

    -- Q1: Challenges faced
    ChallengesFaced         NVARCHAR(MAX) NULL,

    -- Q2: Other activities
    OtherActivities         NVARCHAR(MAX) NULL,

    -- Notes
    Notes                   NVARCHAR(MAX) NULL,

    CONSTRAINT FK_GeneralReports_DeptReports 
        FOREIGN KEY (DepartmentReportId) REFERENCES DepartmentReports(DepartmentReportId)
);
GO

-- ============================================================
-- SECTION 13: REPORT ATTACHMENTS
-- Photos/images referenced in the format doc
-- Stored as file paths or URLs, linked to a DepartmentReport
-- ============================================================

CREATE TABLE ReportAttachments (
    AttachmentId            INT IDENTITY(1,1) PRIMARY KEY,
    DepartmentReportId      INT           NOT NULL,
    FileName                NVARCHAR(255) NOT NULL,
    FilePath                NVARCHAR(500) NOT NULL,  -- relative path or blob URL
    FileType                NVARCHAR(50)  NOT NULL,  -- 'image/jpeg', 'image/png', etc.
    FileSizeBytes           INT           NULL,
    UploadedBy              INT           NOT NULL,  -- MemberId
    UploadedAt              DATETIME2     NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_Attachments_DeptReports 
        FOREIGN KEY (DepartmentReportId) REFERENCES DepartmentReports(DepartmentReportId)
);
GO

-- ============================================================
-- SECTION 14: NOTIFICATIONS
-- In-app reminder and status update notifications
-- ============================================================

CREATE TABLE Notifications (
    NotificationId      INT IDENTITY(1,1) PRIMARY KEY,
    RecipientMemberId   INT           NOT NULL,  -- MemberId from AmsaDB
    Type                NVARCHAR(50)  NOT NULL,
    -- 'ReminderDue' | 'ReportSubmitted' | 'ReportApproved' 
    -- 'ReportRejected' | 'DeadlineApproaching' | 'CycleLocked'

    Title               NVARCHAR(200) NOT NULL,
    Message             NVARCHAR(MAX) NOT NULL,
    ReportId            INT           NULL,  -- linked report if relevant
    CycleId             INT           NULL,  -- linked cycle if relevant
    IsRead              BIT           NOT NULL DEFAULT 0,
    CreatedAt           DATETIME2     NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_Notifications_Reports FOREIGN KEY (ReportId) REFERENCES Reports(ReportId),
    CONSTRAINT FK_Notifications_Cycles  FOREIGN KEY (CycleId) REFERENCES ReportingCycles(CycleId),
    CONSTRAINT CHK_Notifications_Type CHECK (Type IN (
        'ReminderDue', 'ReportSubmitted', 'ReportApproved',
        'ReportRejected', 'DeadlineApproaching', 'CycleLocked'
    ))
);
GO

-- ============================================================
-- SECTION 15: REPORT ACTIVITY LOG
-- Audit trail — every status change recorded
-- ============================================================

CREATE TABLE ReportActivityLog (
    LogId           INT IDENTITY(1,1) PRIMARY KEY,
    ReportId        INT           NOT NULL,
    ActionBy        INT           NOT NULL,  -- MemberId
    Action          NVARCHAR(100) NOT NULL,
    -- e.g. 'DepartmentSubmitted:Taleem', 'PresidentApproved', 
    --      'ForwardedToState', 'StateApproved', 'NationalAcknowledged'
    Notes           NVARCHAR(MAX) NULL,
    ActionAt        DATETIME2     NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_ActivityLog_Reports FOREIGN KEY (ReportId) REFERENCES Reports(ReportId)
);
GO

-- ============================================================
-- SECTION 16: INDEXES
-- Performance indexes for common query patterns
-- ============================================================

-- Reports: most common lookups
CREATE INDEX IX_Reports_CycleId         ON Reports(CycleId);
CREATE INDEX IX_Reports_UnitId          ON Reports(UnitId);
CREATE INDEX IX_Reports_Status          ON Reports(Status);
CREATE INDEX IX_Reports_UnitId_CycleId  ON Reports(UnitId, CycleId);

-- DepartmentReports: lookup by report + department
CREATE INDEX IX_DeptReports_ReportId    ON DepartmentReports(ReportId);
CREATE INDEX IX_DeptReports_Department  ON DepartmentReports(Department);

-- Notifications: unread per member
CREATE INDEX IX_Notifications_Recipient_IsRead 
    ON Notifications(RecipientMemberId, IsRead);

-- Activity log: audit trail per report
CREATE INDEX IX_ActivityLog_ReportId    ON ReportActivityLog(ReportId);

-- Reporting cycles: by month
CREATE INDEX IX_Cycles_ReportMonth      ON ReportingCycles(ReportMonth);
GO

-- ============================================================
-- SECTION 17: EF CORE MIGRATION HISTORY TABLE
-- Needed if using EF Core migrations on this DB
-- (EF Core creates this automatically but documenting it here)
-- ============================================================
-- EF Core will create: __EFMigrationsHistory (MigrationId, ProductVersion)
-- Do NOT manually create this — let EF handle it

-- ============================================================
-- SECTION 18: SEED DATA
-- Initial reporting cycle for testing
-- ============================================================

-- Uncomment to seed a test cycle:
-- INSERT INTO ReportingCycles (ReportMonth, SubmissionDeadline, CreatedBy)
-- VALUES ('2025-01', '2025-02-07 23:59:59', 1);
-- GO

PRINT 'AmsaReportingDB schema created successfully.';
GO
-- ============================================================
-- AMSA REPORTING SYSTEM — STATE REPORTS SCHEMA ADDITION
-- Add these tables to AmsaReportingDB
-- Run after the initial AmsaReporting_Schema.sql
-- ============================================================

-- ============================================================
-- STATE REPORTS
-- One per State per ReportingCycle.
-- State GS/President submits this directly.
-- Simpler than unit reports — no department breakdown.
-- ============================================================

CREATE TABLE StateReports (
    StateReportId       INT IDENTITY(1,1) PRIMARY KEY,
    CycleId             INT           NOT NULL,
    StateId             INT           NOT NULL,  -- FK → AmsaDB.dbo.States (app-level)
    SubmittedBy         INT           NOT NULL,  -- MemberId (AmsaDB)

    Status              NVARCHAR(50)  NOT NULL DEFAULT 'Draft',
    -- 'Draft' | 'SubmittedToNational' | 'Acknowledged'

    -- Q1: Unit president attendance at state meeting
    UnitPresidentsAttended  INT       NOT NULL DEFAULT 0,
    TotalUnitPresidents     INT       NOT NULL DEFAULT 0,
    -- e.g. 4 out of 5

    -- Q2: Unit performance rating + improvement plan
    UnitPerformanceRating   INT       NULL,      -- e.g. 50 (out of 100)
    UnitPerformanceComment  NVARCHAR(MAX) NULL,  -- "how would you help them perform better"

    -- Q3: State-level programs and activities
    -- Stored as structured JSON array OR rich text
    -- We store as rich text for simplicity; each program 
    -- is separated by the officer in their narrative
    ProgramsAndActivities   NVARCHAR(MAX) NULL,

    -- Q4: Challenges + escalation to National
    ChallengesFaced         NVARCHAR(MAX) NULL,
    NationalSupportNeeded   NVARCHAR(MAX) NULL,  -- "how do you need national to come in"

    -- Q5: Other notable items
    OtherNotableItems       NVARCHAR(MAX) NULL,

    -- National acknowledgement
    NationalNote            NVARCHAR(MAX) NULL,
    NationalAcknowledgedBy  INT           NULL,  -- MemberId
    NationalAcknowledgedAt  DATETIME2     NULL,

    CreatedAt   DATETIME2   NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2   NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_StateReports_Cycles FOREIGN KEY (CycleId) 
        REFERENCES ReportingCycles(CycleId),
    CONSTRAINT UQ_StateReports_State_Cycle UNIQUE (StateId, CycleId),
    CONSTRAINT CHK_StateReports_Status CHECK (Status IN (
        'Draft', 'SubmittedToNational', 'Acknowledged'
    )),
    CONSTRAINT CHK_StateReports_Rating CHECK (
        UnitPerformanceRating IS NULL OR 
        (UnitPerformanceRating >= 0 AND UnitPerformanceRating <= 100)
    )
);
GO

-- ============================================================
-- STATE REPORT PROGRAMS
-- Q3 can have multiple programs — each with objectives + outcomes
-- Normalised so we can query/aggregate program data later
-- ============================================================

CREATE TABLE StateReportPrograms (
    ProgramId           INT IDENTITY(1,1) PRIMARY KEY,
    StateReportId       INT           NOT NULL,

    ProgramName         NVARCHAR(200) NOT NULL,  -- e.g. "6th Annual Grand Ramadhan Lecture"
    Objectives          NVARCHAR(MAX) NOT NULL,
    Outcomes            NVARCHAR(MAX) NOT NULL,
    TotalAttendance     INT           NULL,       -- total people who attended
    TotalBeneficiaries  INT           NULL,       -- e.g. 100 from free medical outreach

    CONSTRAINT FK_StateReportPrograms_StateReports 
        FOREIGN KEY (StateReportId) REFERENCES StateReports(StateReportId)
);
GO

-- ============================================================
-- STATE REPORT ATTACHMENTS
-- Same pattern as unit report attachments
-- ============================================================

CREATE TABLE StateReportAttachments (
    AttachmentId        INT IDENTITY(1,1) PRIMARY KEY,
    StateReportId       INT           NOT NULL,

    FileName            NVARCHAR(255) NOT NULL,
    FilePath            NVARCHAR(500) NOT NULL,
    FileType            NVARCHAR(50)  NOT NULL,
    FileSizeBytes       INT           NULL,
    UploadedBy          INT           NOT NULL,  -- MemberId
    UploadedAt          DATETIME2     NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_StateAttachments_StateReports 
        FOREIGN KEY (StateReportId) REFERENCES StateReports(StateReportId)
);
GO

-- ============================================================
-- STATE REPORT ACTIVITY LOG
-- Same audit trail pattern as unit reports
-- ============================================================

CREATE TABLE StateReportActivityLog (
    LogId           INT IDENTITY(1,1) PRIMARY KEY,
    StateReportId   INT           NOT NULL,
    ActionBy        INT           NOT NULL,   -- MemberId
    Action          NVARCHAR(100) NOT NULL,
    -- e.g. 'StateReportSubmitted', 'NationalAcknowledged'
    Notes           NVARCHAR(MAX) NULL,
    ActionAt        DATETIME2     NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_StateActivityLog_StateReports 
        FOREIGN KEY (StateReportId) REFERENCES StateReports(StateReportId)
);
GO

-- ============================================================
-- INDEXES FOR STATE TABLES
-- ============================================================

CREATE INDEX IX_StateReports_CycleId        ON StateReports(CycleId);
CREATE INDEX IX_StateReports_StateId        ON StateReports(StateId);
CREATE INDEX IX_StateReports_Status         ON StateReports(Status);
CREATE INDEX IX_StateReportPrograms_Report  ON StateReportPrograms(StateReportId);
CREATE INDEX IX_StateActivityLog_Report     ON StateReportActivityLog(StateReportId);
GO

PRINT 'State report tables added to AmsaReportingDB successfully.';
GO
