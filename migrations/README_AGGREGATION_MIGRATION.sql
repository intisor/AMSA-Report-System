-- Migration: Add aggregation metadata to StateReport and IsAutoAggregated to StateReportProgram
-- Run this in your DB migration pipeline or create an EF Core migration with equivalent changes.

ALTER TABLE StateReports
ADD IsAggregated BIT NOT NULL DEFAULT 0,
    LastAggregatedAt DATETIME NULL,
    LastAggregatedByMemberId INT NULL;

ALTER TABLE StateReportPrograms
ADD IsAutoAggregated BIT NOT NULL DEFAULT 1;

-- Notes:
-- 1) Existing StateReportPrograms will have IsAutoAggregated = 1 by default; if you want to mark current rows as manual, update accordingly.
-- 2) If using EF Core Migrations, create a new migration that adds these columns and updates the model snapshots.
-- 3) Backups recommended before applying migrations in production.
