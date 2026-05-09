using Microsoft.EntityFrameworkCore;
using AMSAReportingSystem.Data.Entities;

namespace AMSAReportingSystem.Data;

public class AMSAReportingDbContext : DbContext
{
    public AMSAReportingDbContext(DbContextOptions<AMSAReportingDbContext> options)
        : base(options)
    {
    }

    // Core entities
    public DbSet<ReportingCycle> ReportingCycles { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<DepartmentReport> DepartmentReports { get; set; }

    // State reports
    public DbSet<StateReport> StateReports { get; set; }
    public DbSet<StateReportProgram> StateReportPrograms { get; set; }
    public DbSet<StateReportActivity> StateReportActivities { get; set; }
    public DbSet<StateReportAttachment> StateReportAttachments { get; set; }
    public DbSet<StateReportActivityLog> StateReportActivityLogs { get; set; }
    public DbSet<StateReportDepartmentData> StateReportDepartmentData { get; set; }

    // Supporting entities
    public DbSet<ReportActivityLog> ReportActivityLogs { get; set; }
    public DbSet<ReportAttachment> ReportAttachments { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<ComplianceCheck> ComplianceChecks { get; set; }
    public DbSet<Reminder> Reminders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ===== ReportingCycle Configuration =====
        modelBuilder.Entity<ReportingCycle>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CycleMonth).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.CycleMonth).IsUnique();
            entity.HasMany(e => e.Reports)
                .WithOne(r => r.Cycle)
                .HasForeignKey(r => r.CycleId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.StateReports)
                .WithOne(sr => sr.Cycle)
                .HasForeignKey(sr => sr.CycleId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.Notifications)
                .WithOne(n => n.Cycle)
                .HasForeignKey(n => n.CycleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== Report Configuration =====
        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UnitId, e.CycleId }).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.UnitId);
            entity.HasIndex(e => e.StateId);
            entity.HasIndex(e => e.CycleId);
            entity.Property(e => e.PresidentialNotes).HasMaxLength(1000);
            entity.Property(e => e.StateNotes).HasMaxLength(1000);
            entity.Property(e => e.NationalNotes).HasMaxLength(1000);
            entity.HasMany(e => e.DepartmentReports)
                .WithOne(dr => dr.Report)
                .HasForeignKey(dr => dr.ReportId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.ActivityLogs)
                .WithOne(al => al.Report)
                .HasForeignKey(al => al.ReportId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Notifications)
                .WithOne(n => n.Report)
                .HasForeignKey(n => n.ReportId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== DepartmentReport Configuration =====
        modelBuilder.Entity<DepartmentReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReportId);
            entity.HasIndex(e => e.CycleId);
            entity.HasIndex(e => new { e.ReportId, e.Department }).IsUnique();
            entity.Property(e => e.ReportData).HasColumnType("nvarchar(max)");
            entity.Property(e => e.DuesCollected).HasPrecision(18, 2);
            entity.Property(e => e.ExpectedDues).HasPrecision(18, 2);
        });

        // ===== StateReport Configuration =====
        modelBuilder.Entity<StateReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StateId, e.CycleId }).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StateId);
            entity.HasIndex(e => e.CycleId);
            entity.Property(e => e.UnitImprovementPlan).HasMaxLength(2000);
            entity.Property(e => e.ChallengesFaced).HasMaxLength(2000);
            entity.Property(e => e.NationalSupportNeeded).HasMaxLength(2000);
            entity.Property(e => e.UnitPerformanceRating);
            entity.HasMany(e => e.Activities)
                .WithOne(a => a.StateReport)
                .HasForeignKey(a => a.StateReportId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Programs)
                .WithOne(p => p.StateReport)
                .HasForeignKey(p => p.StateReportId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Attachments)
                .WithOne(a => a.StateReport)
                .HasForeignKey(a => a.StateReportId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.ActivityLogs)
                .WithOne(al => al.StateReport)
                .HasForeignKey(al => al.StateReportId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== StateReportProgram Configuration =====
        modelBuilder.Entity<StateReportProgram>(entity =>
        {
            entity.HasKey(e => e.ProgramId);
            entity.HasIndex(e => e.StateReportId);
            entity.Property(e => e.ProgramName).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Objectives).HasMaxLength(1000);
            entity.Property(e => e.Outcomes).HasMaxLength(1000);
        });

        // ===== StateReportActivity Configuration =====
        modelBuilder.Entity<StateReportActivity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ActivityTitle).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Objectives).HasMaxLength(1000);
            entity.Property(e => e.Outcomes).HasMaxLength(1000);
        });

        // ===== StateReportAttachment Configuration =====
        modelBuilder.Entity<StateReportAttachment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FileType).IsRequired().HasMaxLength(50);
        });

        // ===== StateReportActivityLog Configuration =====
        modelBuilder.Entity<StateReportActivityLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.StateReportId);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
        });

        // ===== StateReportDepartmentData Configuration =====
        modelBuilder.Entity<StateReportDepartmentData>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StateReportId, e.DepartmentReportId }).IsUnique();
            entity.HasOne(e => e.StateReport)
                .WithMany(sr => sr.DepartmentDataLinks)
                .HasForeignKey(e => e.StateReportId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.DepartmentReport)
                .WithMany(dr => dr.StateReportDataLinks)
                .HasForeignKey(e => e.DepartmentReportId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== ReportActivityLog Configuration =====
        modelBuilder.Entity<ReportActivityLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReportId);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
        });

        // ===== ReportAttachment Configuration =====
        modelBuilder.Entity<ReportAttachment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FileType).IsRequired().HasMaxLength(50);
        });

        // ===== Notification Configuration =====
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RecipientMemberId, e.IsRead });
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
        });

        // ===== ComplianceCheck Configuration =====
        modelBuilder.Entity<ComplianceCheck>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ComplianceNotes).IsRequired().HasMaxLength(500);
        });

        // ===== Reminder Configuration =====
        modelBuilder.Entity<Reminder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CycleId);
            entity.HasIndex(e => new { e.IsProcessed, e.ScheduledFor });
            entity.Property(e => e.ReminderType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.TargetRole).HasMaxLength(50);
            entity.Property(e => e.TargetState).HasMaxLength(10);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
        });

        // Seed initial data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        var seedTimestamp = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Seed a sample reporting cycle for January 2025
        modelBuilder.Entity<ReportingCycle>().HasData(
            new ReportingCycle
            {
                Id = 1,
                CycleMonth = "January 2025",
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 1, 31),
                SubmissionDeadline = new DateTime(2025, 2, 7),
                IsLocked = false,
                CreatedAt = seedTimestamp,
                UpdatedAt = seedTimestamp
            }
        );
    }
}
