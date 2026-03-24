// ============================================================
// AMSA REPORTING SYSTEM — COMPLETE EF CORE MODELS
// File: AmsaReporting.Data / ReportingModels.cs
// ============================================================
// CROSS-DB NOTE FOR COPILOT:
// UnitId, StateId, MemberId are integers referencing AmsaDB.
// They are NOT foreign keys in this DB — no cross-DB FK constraints.
// Validation against AmsaDB happens at the application/service layer
// by calling the AMSA API before any write operation.
// ============================================================

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AmsaReporting.Data;

// ============================================================
// CONSTANTS — single source of truth for string enums
// ============================================================

public static class ReportStatus
{
    public const string Draft                = "Draft";
    public const string SubmittedToPresident = "SubmittedToPresident";
    public const string ApprovedByPresident  = "ApprovedByPresident";
    public const string SubmittedToState     = "SubmittedToState";
    public const string ApprovedByState      = "ApprovedByState";
    public const string SubmittedToNational  = "SubmittedToNational";
    public const string Acknowledged         = "Acknowledged";

    public static readonly string[] All =
    [
        Draft, SubmittedToPresident, ApprovedByPresident,
        SubmittedToState, ApprovedByState, SubmittedToNational, Acknowledged
    ];
}

public static class StateReportStatus
{
    public const string Draft               = "Draft";
    public const string SubmittedToNational = "SubmittedToNational";
    public const string Acknowledged        = "Acknowledged";
}

public static class DepartmentReportStatus
{
    public const string Draft     = "Draft";
    public const string Submitted = "Submitted";
}

public static class DepartmentType
{
    public const string Taleem          = "Taleem";
    public const string Tabligh         = "Tabligh";
    public const string Welfare         = "Welfare";
    public const string Sport           = "Sport";
    public const string Finance         = "Finance";
    public const string Health          = "Health";
    public const string SecondarySchool = "SecondarySchool";
    public const string Tajneed         = "Tajneed";
    public const string General         = "General";

    public static readonly string[] All =
    [
        Taleem, Tabligh, Welfare, Sport, Finance,
        Health, SecondarySchool, Tajneed, General
    ];
}

public static class NotificationType
{
    public const string ReminderDue         = "ReminderDue";
    public const string ReportSubmitted     = "ReportSubmitted";
    public const string ReportApproved      = "ReportApproved";
    public const string ReportRejected      = "ReportRejected";
    public const string DeadlineApproaching = "DeadlineApproaching";
    public const string CycleLocked         = "CycleLocked";
}

// ============================================================
// REPORTING CYCLE
// Created by National GS. Controls the monthly window.
// ============================================================
public class ReportingCycle
{
    public int CycleId { get; set; }
    public string ReportMonth { get; set; } = null!;          // 'YYYY-MM'
    public DateTime SubmissionDeadline { get; set; }
    public DateTime? ReminderSentAt { get; set; }
    public bool IsLocked { get; set; } = false;
    public int CreatedBy { get; set; }                        // MemberId (AmsaDB)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
    public virtual ICollection<StateReport> StateReports { get; set; } = new List<StateReport>();
}

// ============================================================
// UNIT REPORT
// One per Unit per ReportingCycle.
// Moves up the chain: Unit → State → National
// ============================================================
public class Report
{
    public int ReportId { get; set; }
    public int CycleId { get; set; }
    public int UnitId { get; set; }                           // AmsaDB.Units
    public int SubmittedBy { get; set; }                      // MemberId who created draft
    public string Status { get; set; } = ReportStatus.Draft;

    public string? PresidentialNote { get; set; }
    public string? StateNote { get; set; }
    public string? NationalNote { get; set; }

    public int? PresidentApprovedBy { get; set; }
    public DateTime? PresidentApprovedAt { get; set; }
    public int? StateApprovedBy { get; set; }
    public DateTime? StateApprovedAt { get; set; }
    public int? NationalAcknowledgedBy { get; set; }
    public DateTime? NationalAcknowledgedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ReportingCycle Cycle { get; set; } = null!;
    public virtual ICollection<DepartmentReport> DepartmentReports { get; set; } = new List<DepartmentReport>();
    public virtual ICollection<ReportActivityLog> ActivityLogs { get; set; } = new List<ReportActivityLog>();
}

// ============================================================
// DEPARTMENT REPORT
// One per department per Report.
// Each officer submits their own section independently.
// ============================================================
public class DepartmentReport
{
    public int DepartmentReportId { get; set; }
    public int ReportId { get; set; }
    public string Department { get; set; } = null!;           // DepartmentType constant
    public int? SubmittedBy { get; set; }                     // MemberId of dept officer
    public string Status { get; set; } = DepartmentReportStatus.Draft;
    public DateTime? SubmittedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual Report Report { get; set; } = null!;

    // Exactly one of these will be populated per DepartmentReport
    public virtual TaleemReport? TaleemReport { get; set; }
    public virtual TablighReport? TablighReport { get; set; }
    public virtual WelfareReport? WelfareReport { get; set; }
    public virtual SportReport? SportReport { get; set; }
    public virtual FinanceReport? FinanceReport { get; set; }
    public virtual HealthReport? HealthReport { get; set; }
    public virtual SecondarySchoolReport? SecondarySchoolReport { get; set; }
    public virtual TajneedReport? TajneedReport { get; set; }
    public virtual GeneralReport? GeneralReport { get; set; }
    public virtual ICollection<ReportAttachment> Attachments { get; set; } = new List<ReportAttachment>();
}

// ============================================================
// TALEEM REPORT
// Min standard: 4 sessions, 75% attendance, 1 mosque session (Notes)
// ============================================================
public class TaleemReport
{
    public int TaleemReportId { get; set; }
    public int DepartmentReportId { get; set; }

    public int AverageAttendanceCount { get; set; }
    public int TotalMemberCount { get; set; }
    public string? AttendanceBelowThresholdReason { get; set; } // Required if < 75%
    public int SessionDurationMinutes { get; set; }
    public int MonthlyTestCount { get; set; }
    public int SessionsOrganized { get; set; }
    public string? Notes { get; set; }                        // Mosque session info goes here

    // Set by ComplianceEngine on submission
    public bool IsSessionsCompliant { get; set; } = false;   // sessions >= 4
    public bool IsAttendanceCompliant { get; set; } = false; // attendance% >= 75

    public virtual DepartmentReport DepartmentReport { get; set; } = null!;

    // Not stored — computed on the fly
    public double AttendancePercentage =>
        TotalMemberCount > 0
            ? Math.Round((double)AverageAttendanceCount / TotalMemberCount * 100, 2)
            : 0;
}

// ============================================================
// TABLIGH REPORT
// Min standard: at least 1 on-campus exercise
// ============================================================
public class TablighReport
{
    public int TablighReportId { get; set; }
    public int DepartmentReportId { get; set; }

    public string ActivitiesDetail { get; set; } = null!;
    public string Purpose { get; set; } = null!;
    public int AttendanceCount { get; set; }
    public string Beneficiaries { get; set; } = null!;
    public string? Notes { get; set; }

    public bool HasOnCampusActivity { get; set; } = false;
    public bool IsCompliant { get; set; } = false;

    public virtual DepartmentReport DepartmentReport { get; set; } = null!;
}

// ============================================================
// WELFARE REPORT
// Min standard: 2 activities, 1 must be for members
// ============================================================
public class WelfareReport
{
    public int WelfareReportId { get; set; }
    public int DepartmentReportId { get; set; }

    public int ProgramCount { get; set; }
    public int ParticipantCount { get; set; }
    public string ActivitiesWithDates { get; set; } = null!;
    public string BriefReport { get; set; } = null!;
    public string? Notes { get; set; }

    public bool IsCompliant { get; set; } = false;           // programs >= 2

    public virtual DepartmentReport DepartmentReport { get; set; } = null!;
}

// ============================================================
// SPORT REPORT
// Min standard: 2 activities, 75% member attendance
// ============================================================
public class SportReport
{
    public int SportReportId { get; set; }
    public int DepartmentReportId { get; set; }

    public string GamesPlayed { get; set; } = null!;
    public int MemberParticipantCount { get; set; }
    public int NonMemberParticipantCount { get; set; }
    public int TotalMemberCount { get; set; }
    public string? Notes { get; set; }

    public bool IsAttendanceCompliant { get; set; } = false; // member attendance >= 75%

    public virtual DepartmentReport DepartmentReport { get; set; } = null!;

    public double MemberAttendancePercentage =>
        TotalMemberCount > 0
            ? Math.Round((double)MemberParticipantCount / TotalMemberCount * 100, 2)
            : 0;
}

// ============================================================
// FINANCE REPORT
// Officer inputs expected dues manually
// ============================================================
public class FinanceReport
{
    public int FinanceReportId { get; set; }
    public int DepartmentReportId { get; set; }

    public decimal DuesCollected { get; set; }
    public decimal ExpectedDuesAmount { get; set; }
    public string? DefaultersReason { get; set; }             // Required if not fully collected
    public string? Notes { get; set; }

    public decimal? CollectionRate { get; set; }              // Computed: (collected/expected)*100
    public bool IsFullyCollected { get; set; } = false;

    public virtual DepartmentReport DepartmentReport { get; set; } = null!;
}

// ============================================================
// HEALTH REPORT
// ============================================================
public class HealthReport
{
    public int HealthReportId { get; set; }
    public int DepartmentReportId { get; set; }

    public string ActivitiesWithDates { get; set; } = null!;
    public int BeneficiaryCount { get; set; }
    public string? Notes { get; set; }

    public virtual DepartmentReport DepartmentReport { get; set; } = null!;
}

// ============================================================
// SECONDARY SCHOOL REPORT
// ============================================================
public class SecondarySchoolReport
{
    public int SecondarySchoolReportId { get; set; }
    public int DepartmentReportId { get; set; }

    public bool AttendedJamaatMeeting { get; set; }
    public string? ParentSupportEfforts { get; set; }
    public string JambiteParticipation { get; set; } = null!;
    public string? Notes { get; set; }

    public virtual DepartmentReport DepartmentReport { get; set; } = null!;
}

// ============================================================
// TAJNEED REPORT (Member Registry — AGS responsibility)
// ============================================================
public class TajneedReport
{
    public int TajneedReportId { get; set; }
    public int DepartmentReportId { get; set; }

    public bool HasAccurateTajneed { get; set; }
    public string ImprovementEfforts { get; set; } = null!;
    public string AccuracyPlans { get; set; } = null!;
    public string? Notes { get; set; }

    public virtual DepartmentReport DepartmentReport { get; set; } = null!;
}

// ============================================================
// GENERAL / ADDITIONAL REPORT
// ============================================================
public class GeneralReport
{
    public int GeneralReportId { get; set; }
    public int DepartmentReportId { get; set; }

    public string? ChallengesFaced { get; set; }
    public string? OtherActivities { get; set; }
    public string? Notes { get; set; }

    public virtual DepartmentReport DepartmentReport { get; set; } = null!;
}

// ============================================================
// REPORT ATTACHMENT (Unit-level)
// ============================================================
public class ReportAttachment
{
    public int AttachmentId { get; set; }
    public int DepartmentReportId { get; set; }

    public string FileName { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public string FileType { get; set; } = null!;
    public int? FileSizeBytes { get; set; }
    public int UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public virtual DepartmentReport DepartmentReport { get; set; } = null!;
}

// ============================================================
// UNIT REPORT ACTIVITY LOG
// ============================================================
public class ReportActivityLog
{
    public int LogId { get; set; }
    public int ReportId { get; set; }
    public int ActionBy { get; set; }
    public string Action { get; set; } = null!;
    public string? Notes { get; set; }
    public DateTime ActionAt { get; set; } = DateTime.UtcNow;

    public virtual Report Report { get; set; } = null!;
}

// ============================================================
// NOTIFICATIONS
// ============================================================
public class Notification
{
    public int NotificationId { get; set; }
    public int RecipientMemberId { get; set; }
    public string Type { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public int? ReportId { get; set; }
    public int? StateReportId { get; set; }
    public int? CycleId { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Report? Report { get; set; }
    public virtual StateReport? StateReport { get; set; }
    public virtual ReportingCycle? Cycle { get; set; }
}

// ============================================================
// STATE REPORT
// One per State per ReportingCycle.
// Submitted directly by State GS/President to National.
// ============================================================
public class StateReport
{
    public int StateReportId { get; set; }
    public int CycleId { get; set; }
    public int StateId { get; set; }                          // AmsaDB.States
    public int SubmittedBy { get; set; }
    public string Status { get; set; } = StateReportStatus.Draft;

    // Q1: Unit president attendance at state meeting
    public int UnitPresidentsAttended { get; set; } = 0;
    public int TotalUnitPresidents { get; set; } = 0;

    // Q2: Unit performance rating + plan
    public int? UnitPerformanceRating { get; set; }           // 0–100
    public string? UnitPerformanceComment { get; set; }

    // Q3: State-level programs (also see StateReportPrograms collection)
    public string? ProgramsAndActivities { get; set; }        // Free-text overflow

    // Q4: Challenges + what National can do
    public string? ChallengesFaced { get; set; }
    public string? NationalSupportNeeded { get; set; }

    // Q5: Other notable items
    public string? OtherNotableItems { get; set; }

    // National acknowledgement
    public string? NationalNote { get; set; }
    public int? NationalAcknowledgedBy { get; set; }
    public DateTime? NationalAcknowledgedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ReportingCycle Cycle { get; set; } = null!;
    public virtual ICollection<StateReportProgram> Programs { get; set; } = new List<StateReportProgram>();
    public virtual ICollection<StateReportAttachment> Attachments { get; set; } = new List<StateReportAttachment>();
    public virtual ICollection<StateReportActivityLog> ActivityLogs { get; set; } = new List<StateReportActivityLog>();

    // Not stored
    public string AttendanceSummary =>
        TotalUnitPresidents > 0
            ? $"{UnitPresidentsAttended} out of {TotalUnitPresidents}"
            : "Not recorded";

    public double AttendanceRate =>
        TotalUnitPresidents > 0
            ? Math.Round((double)UnitPresidentsAttended / TotalUnitPresidents * 100, 2)
            : 0;
}

// ============================================================
// STATE REPORT PROGRAM
// Each state-level program is a separate row so National
// can query and aggregate across states (total attendance,
// total beneficiaries, types of programs run etc.)
// ============================================================
public class StateReportProgram
{
    public int ProgramId { get; set; }
    public int StateReportId { get; set; }

    public string ProgramName { get; set; } = null!;
    public string Objectives { get; set; } = null!;
    public string Outcomes { get; set; } = null!;
    public int? TotalAttendance { get; set; }
    public int? TotalBeneficiaries { get; set; }

    public virtual StateReport StateReport { get; set; } = null!;
}

// ============================================================
// STATE REPORT ATTACHMENT
// ============================================================
public class StateReportAttachment
{
    public int AttachmentId { get; set; }
    public int StateReportId { get; set; }

    public string FileName { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public string FileType { get; set; } = null!;
    public int? FileSizeBytes { get; set; }
    public int UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public virtual StateReport StateReport { get; set; } = null!;
}

// ============================================================
// STATE REPORT ACTIVITY LOG
// ============================================================
public class StateReportActivityLog
{
    public int LogId { get; set; }
    public int StateReportId { get; set; }
    public int ActionBy { get; set; }
    public string Action { get; set; } = null!;
    public string? Notes { get; set; }
    public DateTime ActionAt { get; set; } = DateTime.UtcNow;

    public virtual StateReport StateReport { get; set; } = null!;
}

// ============================================================
// COMPLETE DB CONTEXT
// ============================================================
public partial class ReportingDbContext : DbContext
{
    public ReportingDbContext() { }

    public ReportingDbContext(DbContextOptions<ReportingDbContext> options)
        : base(options) { }

    // Unit report tables
    public virtual DbSet<ReportingCycle>        ReportingCycles        { get; set; }
    public virtual DbSet<Report>                Reports                { get; set; }
    public virtual DbSet<DepartmentReport>      DepartmentReports      { get; set; }
    public virtual DbSet<TaleemReport>          TaleemReports          { get; set; }
    public virtual DbSet<TablighReport>         TablighReports         { get; set; }
    public virtual DbSet<WelfareReport>         WelfareReports         { get; set; }
    public virtual DbSet<SportReport>           SportReports           { get; set; }
    public virtual DbSet<FinanceReport>         FinanceReports         { get; set; }
    public virtual DbSet<HealthReport>          HealthReports          { get; set; }
    public virtual DbSet<SecondarySchoolReport> SecondarySchoolReports { get; set; }
    public virtual DbSet<TajneedReport>         TajneedReports         { get; set; }
    public virtual DbSet<GeneralReport>         GeneralReports         { get; set; }
    public virtual DbSet<ReportAttachment>      ReportAttachments      { get; set; }
    public virtual DbSet<ReportActivityLog>     ReportActivityLogs     { get; set; }

    // State report tables
    public virtual DbSet<StateReport>            StateReports            { get; set; }
    public virtual DbSet<StateReportProgram>     StateReportPrograms     { get; set; }
    public virtual DbSet<StateReportAttachment>  StateReportAttachments  { get; set; }
    public virtual DbSet<StateReportActivityLog> StateReportActivityLogs { get; set; }

    // Shared
    public virtual DbSet<Notification> Notifications { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlServer("Name=ReportingConnection");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── ReportingCycle ───────────────────────────────────────
        modelBuilder.Entity<ReportingCycle>(entity =>
        {
            entity.HasKey(e => e.CycleId);
            entity.HasIndex(e => e.ReportMonth).IsUnique();
            entity.Property(e => e.ReportMonth).HasMaxLength(7).IsFixedLength();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // ── Report ───────────────────────────────────────────────
        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.ReportId);
            entity.HasIndex(e => new { e.UnitId, e.CycleId }).IsUnique();
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasOne(e => e.Cycle)
                .WithMany(c => c.Reports)
                .HasForeignKey(e => e.CycleId)
                .HasConstraintName("FK_Reports_Cycles");
        });

        // ── DepartmentReport ─────────────────────────────────────
        modelBuilder.Entity<DepartmentReport>(entity =>
        {
            entity.HasKey(e => e.DepartmentReportId);
            entity.HasIndex(e => new { e.ReportId, e.Department }).IsUnique();
            entity.Property(e => e.Department).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasOne(e => e.Report)
                .WithMany(r => r.DepartmentReports)
                .HasForeignKey(e => e.ReportId)
                .HasConstraintName("FK_DeptReports_Reports");
        });

        // ── TaleemReport ─────────────────────────────────────────
        modelBuilder.Entity<TaleemReport>(entity =>
        {
            entity.HasKey(e => e.TaleemReportId);
            entity.Ignore(e => e.AttendancePercentage);
            entity.HasOne(e => e.DepartmentReport)
                .WithOne(d => d.TaleemReport)
                .HasForeignKey<TaleemReport>(e => e.DepartmentReportId)
                .HasConstraintName("FK_TaleemReports_DeptReports");
        });

        // ── TablighReport ────────────────────────────────────────
        modelBuilder.Entity<TablighReport>(entity =>
        {
            entity.HasKey(e => e.TablighReportId);
            entity.HasOne(e => e.DepartmentReport)
                .WithOne(d => d.TablighReport)
                .HasForeignKey<TablighReport>(e => e.DepartmentReportId)
                .HasConstraintName("FK_TablighReports_DeptReports");
        });

        // ── WelfareReport ────────────────────────────────────────
        modelBuilder.Entity<WelfareReport>(entity =>
        {
            entity.HasKey(e => e.WelfareReportId);
            entity.HasOne(e => e.DepartmentReport)
                .WithOne(d => d.WelfareReport)
                .HasForeignKey<WelfareReport>(e => e.DepartmentReportId)
                .HasConstraintName("FK_WelfareReports_DeptReports");
        });

        // ── SportReport ──────────────────────────────────────────
        modelBuilder.Entity<SportReport>(entity =>
        {
            entity.HasKey(e => e.SportReportId);
            entity.Ignore(e => e.MemberAttendancePercentage);
            entity.HasOne(e => e.DepartmentReport)
                .WithOne(d => d.SportReport)
                .HasForeignKey<SportReport>(e => e.DepartmentReportId)
                .HasConstraintName("FK_SportReports_DeptReports");
        });

        // ── FinanceReport ────────────────────────────────────────
        modelBuilder.Entity<FinanceReport>(entity =>
        {
            entity.HasKey(e => e.FinanceReportId);
            entity.Property(e => e.DuesCollected).HasColumnType("decimal(10,2)");
            entity.Property(e => e.ExpectedDuesAmount).HasColumnType("decimal(10,2)");
            entity.Property(e => e.CollectionRate).HasColumnType("decimal(5,2)");
            entity.HasOne(e => e.DepartmentReport)
                .WithOne(d => d.FinanceReport)
                .HasForeignKey<FinanceReport>(e => e.DepartmentReportId)
                .HasConstraintName("FK_FinanceReports_DeptReports");
        });

        // ── HealthReport ─────────────────────────────────────────
        modelBuilder.Entity<HealthReport>(entity =>
        {
            entity.HasKey(e => e.HealthReportId);
            entity.HasOne(e => e.DepartmentReport)
                .WithOne(d => d.HealthReport)
                .HasForeignKey<HealthReport>(e => e.DepartmentReportId)
                .HasConstraintName("FK_HealthReports_DeptReports");
        });

        // ── SecondarySchoolReport ────────────────────────────────
        modelBuilder.Entity<SecondarySchoolReport>(entity =>
        {
            entity.HasKey(e => e.SecondarySchoolReportId);
            entity.HasOne(e => e.DepartmentReport)
                .WithOne(d => d.SecondarySchoolReport)
                .HasForeignKey<SecondarySchoolReport>(e => e.DepartmentReportId)
                .HasConstraintName("FK_SecSchoolReports_DeptReports");
        });

        // ── TajneedReport ────────────────────────────────────────
        modelBuilder.Entity<TajneedReport>(entity =>
        {
            entity.HasKey(e => e.TajneedReportId);
            entity.HasOne(e => e.DepartmentReport)
                .WithOne(d => d.TajneedReport)
                .HasForeignKey<TajneedReport>(e => e.DepartmentReportId)
                .HasConstraintName("FK_TajneedReports_DeptReports");
        });

        // ── GeneralReport ────────────────────────────────────────
        modelBuilder.Entity<GeneralReport>(entity =>
        {
            entity.HasKey(e => e.GeneralReportId);
            entity.HasOne(e => e.DepartmentReport)
                .WithOne(d => d.GeneralReport)
                .HasForeignKey<GeneralReport>(e => e.DepartmentReportId)
                .HasConstraintName("FK_GeneralReports_DeptReports");
        });

        // ── ReportAttachment ─────────────────────────────────────
        modelBuilder.Entity<ReportAttachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId);
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.FileType).HasMaxLength(50);
            entity.HasOne(e => e.DepartmentReport)
                .WithMany(d => d.Attachments)
                .HasForeignKey(e => e.DepartmentReportId)
                .HasConstraintName("FK_Attachments_DeptReports");
        });

        // ── ReportActivityLog ────────────────────────────────────
        modelBuilder.Entity<ReportActivityLog>(entity =>
        {
            entity.HasKey(e => e.LogId);
            entity.Property(e => e.Action).HasMaxLength(100);
            entity.HasIndex(e => e.ReportId);
            entity.HasOne(e => e.Report)
                .WithMany(r => r.ActivityLogs)
                .HasForeignKey(e => e.ReportId)
                .HasConstraintName("FK_ActivityLog_Reports");
        });

        // ── StateReport ──────────────────────────────────────────
        modelBuilder.Entity<StateReport>(entity =>
        {
            entity.HasKey(e => e.StateReportId);
            entity.HasIndex(e => new { e.StateId, e.CycleId }).IsUnique();
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Ignore(e => e.AttendanceSummary);
            entity.Ignore(e => e.AttendanceRate);
            entity.HasOne(e => e.Cycle)
                .WithMany(c => c.StateReports)
                .HasForeignKey(e => e.CycleId)
                .HasConstraintName("FK_StateReports_Cycles");
        });

        // ── StateReportProgram ───────────────────────────────────
        modelBuilder.Entity<StateReportProgram>(entity =>
        {
            entity.HasKey(e => e.ProgramId);
            entity.Property(e => e.ProgramName).HasMaxLength(200);
            entity.HasIndex(e => e.StateReportId);
            entity.HasOne(e => e.StateReport)
                .WithMany(s => s.Programs)
                .HasForeignKey(e => e.StateReportId)
                .HasConstraintName("FK_StateReportPrograms_StateReports");
        });

        // ── StateReportAttachment ────────────────────────────────
        modelBuilder.Entity<StateReportAttachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId);
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.FileType).HasMaxLength(50);
            entity.HasOne(e => e.StateReport)
                .WithMany(s => s.Attachments)
                .HasForeignKey(e => e.StateReportId)
                .HasConstraintName("FK_StateAttachments_StateReports");
        });

        // ── StateReportActivityLog ───────────────────────────────
        modelBuilder.Entity<StateReportActivityLog>(entity =>
        {
            entity.HasKey(e => e.LogId);
            entity.Property(e => e.Action).HasMaxLength(100);
            entity.HasIndex(e => e.StateReportId);
            entity.HasOne(e => e.StateReport)
                .WithMany(s => s.ActivityLogs)
                .HasForeignKey(e => e.StateReportId)
                .HasConstraintName("FK_StateActivityLog_StateReports");
        });

        // ── Notification ─────────────────────────────────────────
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId);
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.HasIndex(e => new { e.RecipientMemberId, e.IsRead });
            entity.HasOne(e => e.Report)
                .WithMany()
                .HasForeignKey(e => e.ReportId)
                .IsRequired(false)
                .HasConstraintName("FK_Notifications_Reports");
            entity.HasOne(e => e.StateReport)
                .WithMany()
                .HasForeignKey(e => e.StateReportId)
                .IsRequired(false)
                .HasConstraintName("FK_Notifications_StateReports");
            entity.HasOne(e => e.Cycle)
                .WithMany()
                .HasForeignKey(e => e.CycleId)
                .IsRequired(false)
                .HasConstraintName("FK_Notifications_Cycles");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
