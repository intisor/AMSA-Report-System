// ============================================================
// AMSA REPORTING SYSTEM — EF CORE ENTITY MODELS
// Namespace: AmsaReporting.Data
// ============================================================
// NOTE FOR COPILOT:
// - These entities map to AmsaReportingDB (separate from AmsaDB)
// - MemberId, UnitId, StateId references point to AmsaDB — 
//   they are NOT foreign keys in this DB, just stored integers
//   (cross-DB FK enforcement is handled at the application layer)
// - All DateTime fields use DateTime2 / UTC
// ============================================================

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AmsaReporting.Data;

// ============================================================
// REPORTING CYCLE
// ============================================================
public class ReportingCycle
{
    public int CycleId { get; set; }
    public string ReportMonth { get; set; } = null!;         // 'YYYY-MM'
    public DateTime SubmissionDeadline { get; set; }
    public DateTime? ReminderSentAt { get; set; }
    public bool IsLocked { get; set; } = false;
    public int CreatedBy { get; set; }                        // MemberId (AmsaDB)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
}

// ============================================================
// REPORT (Unit-level, one per unit per cycle)
// ============================================================
public class Report
{
    public int ReportId { get; set; }
    public int CycleId { get; set; }
    public int UnitId { get; set; }                           // FK → AmsaDB.Units (app-level)
    public int SubmittedBy { get; set; }                      // MemberId (AmsaDB)
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

// Report status constants — single source of truth
public static class ReportStatus
{
    public const string Draft                = "Draft";
    public const string SubmittedToPresident = "SubmittedToPresident";
    public const string ApprovedByPresident  = "ApprovedByPresident";
    public const string SubmittedToState     = "SubmittedToState";
    public const string ApprovedByState      = "ApprovedByState";
    public const string SubmittedToNational  = "SubmittedToNational";
    public const string Acknowledged         = "Acknowledged";
}

// ============================================================
// DEPARTMENT REPORT (one per department per Report)
// ============================================================
public class DepartmentReport
{
    public int DepartmentReportId { get; set; }
    public int ReportId { get; set; }
    public string Department { get; set; } = null!;
    public int? SubmittedBy { get; set; }                     // MemberId (AmsaDB)
    public string Status { get; set; } = DepartmentReportStatus.Draft;
    public DateTime? SubmittedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual Report Report { get; set; } = null!;

    // Navigation to each dept report type (only one will be non-null)
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

// ============================================================
// TALEEM REPORT
// ============================================================
public class TaleemReport
{
    public int TaleemReportId { get; set; }
    public int DepartmentReportId { get; set; }

    public int AverageAttendanceCount { get; set; }
    public int TotalMemberCount { get; set; }
    public string? AttendanceBelowThresholdReason { get; set; }
    public int SessionDurationMinutes { get; set; }
    public int MonthlyTestCount { get; set; }
    public int SessionsOrganized { get; set; }
    public string? Notes { get; set; }

    // Compliance flags — computed by ComplianceEngine on submission
    public bool IsSessionsCompliant { get; set; } = false;    // sessions >= 4
    public bool IsAttendanceCompliant { get; set; } = false;  // attendance% >= 75

    public virtual DepartmentReport DepartmentReport { get; set; } = null!;

    // Computed property — not stored
    public double AttendancePercentage =>
        TotalMemberCount > 0 ? (double)AverageAttendanceCount / TotalMemberCount * 100 : 0;
}

// ============================================================
// TABLIGH REPORT
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
        TotalMemberCount > 0 ? (double)MemberParticipantCount / TotalMemberCount * 100 : 0;
}

// ============================================================
// FINANCE REPORT
// ============================================================
public class FinanceReport
{
    public int FinanceReportId { get; set; }
    public int DepartmentReportId { get; set; }

    public decimal DuesCollected { get; set; }
    public decimal ExpectedDuesAmount { get; set; }           // officer inputs manually
    public string? DefaultersReason { get; set; }             // required if not fully collected
    public string? Notes { get; set; }

    public decimal? CollectionRate { get; set; }              // computed: (collected/expected)*100
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
// TAJNEED REPORT
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
// REPORT ATTACHMENTS
// ============================================================
public class ReportAttachment
{
    public int AttachmentId { get; set; }
    public int DepartmentReportId { get; set; }

    public string FileName { get; set; } = null!;
    public string FilePath { get; set; } = null!;             // relative path or blob URL
    public string FileType { get; set; } = null!;             // 'image/jpeg', 'image/png'
    public int? FileSizeBytes { get; set; }
    public int UploadedBy { get; set; }                       // MemberId (AmsaDB)
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public virtual DepartmentReport DepartmentReport { get; set; } = null!;
}

// ============================================================
// NOTIFICATIONS
// ============================================================
public class Notification
{
    public int NotificationId { get; set; }
    public int RecipientMemberId { get; set; }                // MemberId (AmsaDB)
    public string Type { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public int? ReportId { get; set; }
    public int? CycleId { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Report? Report { get; set; }
    public virtual ReportingCycle? Cycle { get; set; }
}

public static class NotificationType
{
    public const string ReminderDue          = "ReminderDue";
    public const string ReportSubmitted      = "ReportSubmitted";
    public const string ReportApproved       = "ReportApproved";
    public const string ReportRejected       = "ReportRejected";
    public const string DeadlineApproaching  = "DeadlineApproaching";
    public const string CycleLocked         = "CycleLocked";
}

// ============================================================
// REPORT ACTIVITY LOG (Audit Trail)
// ============================================================
public class ReportActivityLog
{
    public int LogId { get; set; }
    public int ReportId { get; set; }
    public int ActionBy { get; set; }                         // MemberId (AmsaDB)
    public string Action { get; set; } = null!;
    // Examples: 'DepartmentSubmitted:Taleem', 'PresidentApproved',
    //           'ForwardedToState', 'StateApproved', 'NationalAcknowledged'
    public string? Notes { get; set; }
    public DateTime ActionAt { get; set; } = DateTime.UtcNow;

    public virtual Report Report { get; set; } = null!;
}

// ============================================================
// DB CONTEXT
// ============================================================
public partial class ReportingDbContext : DbContext
{
    public ReportingDbContext() { }

    public ReportingDbContext(DbContextOptions<ReportingDbContext> options)
        : base(options) { }

    public virtual DbSet<ReportingCycle>        ReportingCycles     { get; set; }
    public virtual DbSet<Report>                Reports             { get; set; }
    public virtual DbSet<DepartmentReport>      DepartmentReports   { get; set; }
    public virtual DbSet<TaleemReport>          TaleemReports       { get; set; }
    public virtual DbSet<TablighReport>         TablighReports      { get; set; }
    public virtual DbSet<WelfareReport>         WelfareReports      { get; set; }
    public virtual DbSet<SportReport>           SportReports        { get; set; }
    public virtual DbSet<FinanceReport>         FinanceReports      { get; set; }
    public virtual DbSet<HealthReport>          HealthReports       { get; set; }
    public virtual DbSet<SecondarySchoolReport> SecondarySchoolReports { get; set; }
    public virtual DbSet<TajneedReport>         TajneedReports      { get; set; }
    public virtual DbSet<GeneralReport>         GeneralReports      { get; set; }
    public virtual DbSet<ReportAttachment>      ReportAttachments   { get; set; }
    public virtual DbSet<Notification>          Notifications       { get; set; }
    public virtual DbSet<ReportActivityLog>     ReportActivityLogs  { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Name=ReportingConnection");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── ReportingCycle ──────────────────────────────────────
        modelBuilder.Entity<ReportingCycle>(entity =>
        {
            entity.HasKey(e => e.CycleId);
            entity.HasIndex(e => e.ReportMonth).IsUnique();
            entity.Property(e => e.ReportMonth).HasMaxLength(7).IsFixedLength();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // ── Report ──────────────────────────────────────────────
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

        // ── DepartmentReport ────────────────────────────────────
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

        // ── TaleemReport ────────────────────────────────────────
        modelBuilder.Entity<TaleemReport>(entity =>
        {
            entity.HasKey(e => e.TaleemReportId);
            entity.HasOne(e => e.DepartmentReport)
                .WithOne(d => d.TaleemReport)
                .HasForeignKey<TaleemReport>(e => e.DepartmentReportId)
                .HasConstraintName("FK_TaleemReports_DeptReports");
            // Exclude computed property from mapping
            entity.Ignore(e => e.AttendancePercentage);
        });

        // ── TablighReport ───────────────────────────────────────
        modelBuilder.Entity<TablighReport>(entity =>
        {
            entity.HasKey(e => e.TablighReportId);
            entity.HasOne(e => e.DepartmentReport)
                .WithOne(d => d.TablighReport)
                .HasForeignKey<TablighReport>(e => e.DepartmentReportId)
                .HasConstraintName("FK_TablighReports_DeptReports");
        });

        // ── WelfareReport ───────────────────────────────────────
        modelBuilder.Entity<WelfareReport>(entity =>
        {
            entity.HasKey(e => e.WelfareReportId);
            entity.HasOne(e => e.DepartmentReport)
                .WithOne(d => d.WelfareReport)
                .HasForeignKey<WelfareReport>(e => e.DepartmentReportId)
                .HasConstraintName("FK_WelfareReports_DeptReports");
        });

        // ── SportReport ─────────────────────────────────────────
        modelBuilder.Entity<SportReport>(entity =>
        {
            entity.HasKey(e => e.SportReportId);
            entity.HasOne(e => e.DepartmentReport)
                .WithOne(d => d.SportReport)
                .HasForeignKey<SportReport>(e => e.DepartmentReportId)
                .HasConstraintName("FK_SportReports_DeptReports");
            entity.Ignore(e => e.MemberAttendancePercentage);
        });

        // ── FinanceReport ───────────────────────────────────────
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

        // ── HealthReport ────────────────────────────────────────
        modelBuilder.Entity<HealthReport>(entity =>
        {
            entity.HasKey(e => e.HealthReportId);
            entity.HasOne(e => e.DepartmentReport)
                .WithOne(d => d.HealthReport)
                .HasForeignKey<HealthReport>(e => e.DepartmentReportId)
                .HasConstraintName("FK_HealthReports_DeptReports");
        });

        // ── SecondarySchoolReport ───────────────────────────────
        modelBuilder.Entity<SecondarySchoolReport>(entity =>
        {
            entity.HasKey(e => e.SecondarySchoolReportId);
            entity.HasOne(e => e.DepartmentReport)
                .WithOne(d => d.SecondarySchoolReport)
                .HasForeignKey<SecondarySchoolReport>(e => e.DepartmentReportId)
                .HasConstraintName("FK_SecSchoolReports_DeptReports");
        });

        // ── TajneedReport ───────────────────────────────────────
        modelBuilder.Entity<TajneedReport>(entity =>
        {
            entity.HasKey(e => e.TajneedReportId);
            entity.HasOne(e => e.DepartmentReport)
                .WithOne(d => d.TajneedReport)
                .HasForeignKey<TajneedReport>(e => e.DepartmentReportId)
                .HasConstraintName("FK_TajneedReports_DeptReports");
        });

        // ── GeneralReport ───────────────────────────────────────
        modelBuilder.Entity<GeneralReport>(entity =>
        {
            entity.HasKey(e => e.GeneralReportId);
            entity.HasOne(e => e.DepartmentReport)
                .WithOne(d => d.GeneralReport)
                .HasForeignKey<GeneralReport>(e => e.DepartmentReportId)
                .HasConstraintName("FK_GeneralReports_DeptReports");
        });

        // ── ReportAttachment ────────────────────────────────────
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

        // ── Notification ────────────────────────────────────────
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
            entity.HasOne(e => e.Cycle)
                .WithMany()
                .HasForeignKey(e => e.CycleId)
                .IsRequired(false)
                .HasConstraintName("FK_Notifications_Cycles");
        });

        // ── ReportActivityLog ───────────────────────────────────
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

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
