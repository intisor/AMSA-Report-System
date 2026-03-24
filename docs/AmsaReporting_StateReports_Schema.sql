-- ============================================================
-- AMSA REPORTING SYSTEM — STATE REPORTS SCHEMA ADDITION
-- Add these tables to AmsaReportingDB
-- Run AFTER the main AmsaReporting_Schema.sql
-- ============================================================

-- ============================================================
-- SECTION A: STATE REPORTS
-- One StateReport per State per ReportingCycle.
-- Submitted by State Executive Officers as a single report.
-- No department breakdown — state reports as one unit.
-- ============================================================

CREATE TABLE StateReports (
    StateReportId       INT IDENTITY(1,1) PRIMARY KEY,
    CycleId             INT           NOT NULL,
    StateId             INT           NOT NULL,  -- FK → AmsaDB.dbo.States (app-level)
    SubmittedBy         INT           NOT NULL,  -- MemberId of submitting officer (AmsaDB)
    Status              NVARCHAR(50)  NOT NULL DEFAULT 'Draft',
    -- Status values:
    -- 'Draft'                → being filled
    -- 'SubmittedToPresident' → submitted, awaiting state president review
    -- 'ApprovedByPresident'  → state president approved
    -- 'SubmittedToNational'  → forwarded to National GS
    -- 'Acknowledged'         → National GS acknowledged

    -- Q1: Unit president attendance at state meeting
    UnitPresidentsAttended  INT       NOT NULL DEFAULT 0,
    TotalUnitPresidents     INT       NOT NULL DEFAULT 0,  -- total units in state

    -- Q2: Unit performance rating + improvement plan
    UnitPerformanceRating   INT       NULL,  -- 0–100 score
    UnitImprovementPlan     NVARCHAR(MAX) NULL,

    -- Q4: Challenges + request for national support
    ChallengesFaced         NVARCHAR(MAX) NULL,
    NationalSupportNeeded   NVARCHAR(MAX) NULL,

    -- Q5: Other noteworthy items
    OtherNotes              NVARCHAR(MAX) NULL,

    -- Approval chain
    PresidentialNote        NVARCHAR(MAX) NULL,
    NationalNote            NVARCHAR(MAX) NULL,

    PresidentApprovedBy     INT       NULL,  -- MemberId (AmsaDB)
    PresidentApprovedAt     DATETIME2 NULL,
    NationalAcknowledgedBy  INT       NULL,  -- MemberId (AmsaDB)
    NationalAcknowledgedAt  DATETIME2 NULL,

    CreatedAt       DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2     NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_StateReports_Cycles FOREIGN KEY (CycleId) 
        REFERENCES ReportingCycles(CycleId),
    CONSTRAINT UQ_StateReports_State_Cycle UNIQUE (StateId, CycleId),  -- one per state per month
    CONSTRAINT CHK_StateReports_Status CHECK (Status IN (
        'Draft', 'SubmittedToPresident', 'ApprovedByPresident',
        'SubmittedToNational', 'Acknowledged'
    )),
    CONSTRAINT CHK_StateReports_Rating CHECK (
        UnitPerformanceRating IS NULL OR 
        (UnitPerformanceRating >= 0 AND UnitPerformanceRating <= 100)
    )
);
GO

-- ============================================================
-- SECTION B: STATE REPORT ACTIVITIES
-- Q3 from the format: programs/activities at state level.
-- Each activity is a separate row (one report can have many).
-- ============================================================

CREATE TABLE StateReportActivities (
    ActivityId          INT IDENTITY(1,1) PRIMARY KEY,
    StateReportId       INT           NOT NULL,

    -- Activity heading (e.g. "6th Annual Grand Ramadhan Lecture")
    ActivityTitle       NVARCHAR(300) NOT NULL,

    -- Objectives (can be multi-line text)
    Objectives          NVARCHAR(MAX) NOT NULL,

    -- Outcomes
    Outcomes            NVARCHAR(MAX) NOT NULL,

    -- Optional metrics captured from outcomes
    AttendanceCount     INT           NULL,  -- e.g. "300 people attended"
    BeneficiaryCount    INT           NULL,  -- e.g. "100 benefited from medical outreach"

    -- Activity date if known
    ActivityDate        DATE          NULL,

    CONSTRAINT FK_StateActivities_StateReports FOREIGN KEY (StateReportId) 
        REFERENCES StateReports(StateReportId)
);
GO

-- ============================================================
-- SECTION C: STATE REPORT ATTACHMENTS
-- Photos/images for state reports
-- ============================================================

CREATE TABLE StateReportAttachments (
    AttachmentId        INT IDENTITY(1,1) PRIMARY KEY,
    StateReportId       INT           NOT NULL,
    FileName            NVARCHAR(255) NOT NULL,
    FilePath            NVARCHAR(500) NOT NULL,
    FileType            NVARCHAR(50)  NOT NULL,
    FileSizeBytes       INT           NULL,
    UploadedBy          INT           NOT NULL,  -- MemberId (AmsaDB)
    UploadedAt          DATETIME2     NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_StateAttachments_StateReports FOREIGN KEY (StateReportId) 
        REFERENCES StateReports(StateReportId)
);
GO

-- ============================================================
-- SECTION D: STATE REPORT ACTIVITY LOG
-- Audit trail for state report status changes
-- ============================================================

CREATE TABLE StateReportActivityLog (
    LogId               INT IDENTITY(1,1) PRIMARY KEY,
    StateReportId       INT           NOT NULL,
    ActionBy            INT           NOT NULL,  -- MemberId (AmsaDB)
    Action              NVARCHAR(100) NOT NULL,
    -- e.g. 'Submitted', 'PresidentApproved', 
    --      'ForwardedToNational', 'NationalAcknowledged'
    Notes               NVARCHAR(MAX) NULL,
    ActionAt            DATETIME2     NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_StateActivityLog_StateReports FOREIGN KEY (StateReportId) 
        REFERENCES StateReports(StateReportId)
);
GO

-- ============================================================
-- SECTION E: INDEXES FOR STATE REPORT TABLES
-- ============================================================

CREATE INDEX IX_StateReports_CycleId        ON StateReports(CycleId);
CREATE INDEX IX_StateReports_StateId        ON StateReports(StateId);
CREATE INDEX IX_StateReports_Status         ON StateReports(Status);
CREATE INDEX IX_StateReports_StateId_Cycle  ON StateReports(StateId, CycleId);
CREATE INDEX IX_StateActivities_StateReport ON StateReportActivities(StateReportId);
CREATE INDEX IX_StateActivityLog_ReportId   ON StateReportActivityLog(StateReportId);
GO

-- ============================================================
-- ALSO: Update Notifications table to support StateReports
-- Add StateReportId column to existing Notifications table
-- ============================================================

ALTER TABLE Notifications
    ADD StateReportId INT NULL
        CONSTRAINT FK_Notifications_StateReports 
        FOREIGN KEY REFERENCES StateReports(StateReportId);
GO

PRINT 'State report tables added to AmsaReportingDB successfully.';
GO
