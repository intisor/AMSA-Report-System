// ============================================================
// AMSA REPORTING SYSTEM — STATE REPORT EF CORE ENTITIES
// Add these to AmsaReporting.Data namespace
// alongside the existing AmsaReporting_EFCore_Models.cs
// ============================================================

using System;
using System.Collections.Generic;

namespace AmsaReporting.Data;

// ============================================================
// STATE REPORT
// One per state per reporting cycle.
// Submitted by state executive officers as a single report —
// no department breakdown at state level.
// ============================================================
public class StateReport
{
    public int StateReportId { get; set; }
    public int CycleId { get; set; }
    public int StateId { get; set; }                          // FK → AmsaDB.States (app-level)
    public int SubmittedBy { get; set; }                      // MemberId (AmsaDB)
    public string Status { get; set; } = StateReportStatus.Draft;

    // Q1: Unit president attendance at state meeting
    public int UnitPresidentsAttended { get; set; } = 0;
    public int TotalUnitPresidents { get; set; } = 0;

    // Q2: Unit performance rating + improvement plan
    public int? UnitPerformanceRating { get; set; }           // 0–100
    public string? UnitImprovementPlan { get; set; }

    // Q4: Challenges + national support request
    public string? ChallengesFaced { get; set; }
    public string? NationalSupportNeeded { get; set; }

    // Q5: Other noteworthy items
    public string? OtherNotes { get; set; }

    // Approval chain notes
    public string? PresidentialNote { get; set; }
    public string? NationalNote { get; set; }

    // Approval tracking
    public int? PresidentApprovedBy { get; set; }
    public DateTime? PresidentApprovedAt { get; set; }
    public int? NationalAcknowledgedBy { get; set; }
    public DateTime? NationalAcknowledgedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual ReportingCycle Cycle { get; set; } = null!;
    public virtual ICollection<StateReportActivity> Activities { get; set; } 
        = new List<StateReportActivity>();
    public virtual ICollection<StateReportAttachment> Attachments { get; set; } 
        = new List<StateReportAttachment>();
    public virtual ICollection<StateReportActivityLog> ActivityLogs { get; set; } 
        = new List<StateReportActivityLog>();

    // Computed — not stored
    public double UnitPresidentAttendanceRate =>
        TotalUnitPresidents > 0
            ? (double)UnitPresidentsAttended / TotalUnitPresidents * 100
            : 0;
}

// Status constants for state reports
public static class StateReportStatus
{
    public const string Draft                = "Draft";
    public const string SubmittedToPresident = "SubmittedToPresident";
    public const string ApprovedByPresident  = "ApprovedByPresident";
    public const string SubmittedToNational  = "SubmittedToNational";
    public const string Acknowledged         = "Acknowledged";
}

// ============================================================
// STATE REPORT ACTIVITY
// Q3: Programs/activities carried out at state level.
// One StateReport can have many activities.
// e.g. "6th Annual Grand Ramadhan Lecture", "Free Medical Outreach"
// ============================================================
public class StateReportActivity
{
    public int ActivityId { get; set; }
    public int StateReportId { get; set; }

    public string ActivityTitle { get; set; } = null!;        // Heading for the activity
    public string Objectives { get; set; } = null!;
    public string Outcomes { get; set; } = null!;

    // Optional quantitative capture from outcomes text
    public int? AttendanceCount { get; set; }
    public int? BeneficiaryCount { get; set; }

    public DateOnly? ActivityDate { get; set; }

    public virtual StateReport StateReport { get; set; } = null!;
}

// ============================================================
// STATE REPORT ATTACHMENT
// Photos/images for state reports
// ============================================================
public class StateReportAttachment
{
    public int AttachmentId { get; set; }
    public int StateReportId { get; set; }

    public string FileName { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public string FileType { get; set; } = null!;
    public int? FileSizeBytes { get; set; }
    public int UploadedBy { get; set; }                       // MemberId (AmsaDB)
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public virtual StateReport StateReport { get; set; } = null!;
}

// ============================================================
// STATE REPORT ACTIVITY LOG
// Audit trail for state report status changes
// ============================================================
public class StateReportActivityLog
{
    public int LogId { get; set; }
    public int StateReportId { get; set; }
    public int ActionBy { get; set; }                         // MemberId (AmsaDB)
    public string Action { get; set; } = null!;
    // Examples: 'Submitted', 'PresidentApproved',
    //           'ForwardedToNational', 'NationalAcknowledged'
    public string? Notes { get; set; }
    public DateTime ActionAt { get; set; } = DateTime.UtcNow;

    public virtual StateReport StateReport { get; set; } = null!;
}

// ============================================================
// DB CONTEXT ADDITIONS
// Add these to ReportingDbContext — the existing OnModelCreating
// method gets a partial extension via OnModelCreatingPartial
// ============================================================

// Add these DbSet properties to ReportingDbContext:
//
//   public virtual DbSet<StateReport>           StateReports            { get; set; }
//   public virtual DbSet<StateReportActivity>   StateReportActivities   { get; set; }
//   public virtual DbSet<StateReportAttachment> StateReportAttachments  { get; set; }
//   public virtual DbSet<StateReportActivityLog> StateReportActivityLogs { get; set; }

// Add this partial class to wire up state report model config:
public partial class ReportingDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // ── StateReport ─────────────────────────────────────────
        modelBuilder.Entity<StateReport>(entity =>
        {
            entity.HasKey(e => e.StateReportId);
            entity.HasIndex(e => new { e.StateId, e.CycleId }).IsUnique();
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Ignore(e => e.UnitPresidentAttendanceRate);

            entity.HasOne(e => e.Cycle)
                .WithMany()
                .HasForeignKey(e => e.CycleId)
                .HasConstraintName("FK_StateReports_Cycles");
        });

        // ── StateReportActivity ─────────────────────────────────
        modelBuilder.Entity<StateReportActivity>(entity =>
        {
            entity.HasKey(e => e.ActivityId);
            entity.Property(e => e.ActivityTitle).HasMaxLength(300);
            entity.HasIndex(e => e.StateReportId);

            entity.HasOne(e => e.StateReport)
                .WithMany(s => s.Activities)
                .HasForeignKey(e => e.StateReportId)
                .HasConstraintName("FK_StateActivities_StateReports");
        });

        // ── StateReportAttachment ───────────────────────────────
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

        // ── StateReportActivityLog ──────────────────────────────
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

        // ── Notification: add StateReportId ────────────────────
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasOne<StateReport>()
                .WithMany()
                .HasForeignKey(e => e.StateReportId)
                .IsRequired(false)
                .HasConstraintName("FK_Notifications_StateReports");
        });
    }
}
