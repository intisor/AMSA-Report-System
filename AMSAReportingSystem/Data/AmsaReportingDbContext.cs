using Microsoft.EntityFrameworkCore;
using AMSAReportingSystem.Data.Entities;

namespace AMSAReportingSystem.Data;

public class AmsaReportingDbContext : DbContext
{
    public AmsaReportingDbContext(DbContextOptions<AmsaReportingDbContext> options) 
        : base(options)
    {
    }

    // Core entities
    public DbSet<ReportingCycle> ReportingCycles { get; set; }
    public DbSet<State> States { get; set; }
    public DbSet<Unit> Units { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<DepartmentReport> DepartmentReports { get; set; }

    // State reports
    public DbSet<StateReport> StateReports { get; set; }
    public DbSet<StateReportActivity> StateReportActivities { get; set; }
    public DbSet<StateReportAttachment> StateReportAttachments { get; set; }
    public DbSet<StateReportActivityLog> StateReportActivityLogs { get; set; }

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

        // ===== State Configuration =====
        modelBuilder.Entity<State>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Abbreviation).IsRequired().HasMaxLength(10);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasMany(e => e.Units)
                .WithOne(u => u.State)
                .HasForeignKey(u => u.StateId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.StateReports)
                .WithOne(sr => sr.State)
                .HasForeignKey(sr => sr.StateId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== Unit Configuration =====
        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PresidentName).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.AmsaDbUnitId).IsUnique();
            entity.HasMany(e => e.Reports)
                .WithOne(r => r.Unit)
                .HasForeignKey(r => r.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== Report Configuration =====
        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UnitId, e.CycleId }).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.UnitId);
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
            entity.HasIndex(e => new { e.ReportId, e.Department }).IsUnique();
            entity.Property(e => e.ReportData).HasColumnType("nvarchar(max)");
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
            entity.HasMany(e => e.Activities)
                .WithOne(a => a.StateReport)
                .HasForeignKey(a => a.StateReportId)
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

        // Seed all 36 Nigerian states + FCT
        var states = new List<State>
        {
            new State { Id = 1, Name = "Abia", Abbreviation = "AB" },
            new State { Id = 2, Name = "Adamawa", Abbreviation = "AD" },
            new State { Id = 3, Name = "Akwa Ibom", Abbreviation = "AK" },
            new State { Id = 4, Name = "Anambra", Abbreviation = "AN" },
            new State { Id = 5, Name = "Bauchi", Abbreviation = "BC" },
            new State { Id = 6, Name = "Bayelsa", Abbreviation = "BY" },
            new State { Id = 7, Name = "Benue", Abbreviation = "BN" },
            new State { Id = 8, Name = "Borno", Abbreviation = "BO" },
            new State { Id = 9, Name = "Cross River", Abbreviation = "CR" },
            new State { Id = 10, Name = "Delta", Abbreviation = "DT" },
            new State { Id = 11, Name = "Ebonyi", Abbreviation = "EB" },
            new State { Id = 12, Name = "Edo", Abbreviation = "ED" },
            new State { Id = 13, Name = "Ekiti", Abbreviation = "EK" },
            new State { Id = 14, Name = "Enugu", Abbreviation = "EN" },
            new State { Id = 15, Name = "FCT", Abbreviation = "FC" },
            new State { Id = 16, Name = "Gombe", Abbreviation = "GM" },
            new State { Id = 17, Name = "Imo", Abbreviation = "IM" },
            new State { Id = 18, Name = "Jigawa", Abbreviation = "JG" },
            new State { Id = 19, Name = "Kaduna", Abbreviation = "KD" },
            new State { Id = 20, Name = "Kano", Abbreviation = "KN" },
            new State { Id = 21, Name = "Katsina", Abbreviation = "KT" },
            new State { Id = 22, Name = "Kebbi", Abbreviation = "KB" },
            new State { Id = 23, Name = "Kogi", Abbreviation = "KG" },
            new State { Id = 24, Name = "Kwara", Abbreviation = "KW" },
            new State { Id = 25, Name = "Lagos", Abbreviation = "LG" },
            new State { Id = 26, Name = "Nasarawa", Abbreviation = "NS" },
            new State { Id = 27, Name = "Niger", Abbreviation = "NG" },
            new State { Id = 28, Name = "Ogun", Abbreviation = "OG" },
            new State { Id = 29, Name = "Ondo", Abbreviation = "OD" },
            new State { Id = 30, Name = "Osun", Abbreviation = "OS" },
            new State { Id = 31, Name = "Oyo", Abbreviation = "OY" },
            new State { Id = 32, Name = "Plateau", Abbreviation = "PL" },
            new State { Id = 33, Name = "Rivers", Abbreviation = "RV" },
            new State { Id = 34, Name = "Sokoto", Abbreviation = "SK" },
            new State { Id = 35, Name = "Taraba", Abbreviation = "TR" },
            new State { Id = 36, Name = "Yobe", Abbreviation = "YB" },
            new State { Id = 37, Name = "Zamfara", Abbreviation = "ZM" }
        };

        foreach (var state in states)
        {
            state.CreatedAt = seedTimestamp;
            state.UpdatedAt = seedTimestamp;
        }

        modelBuilder.Entity<State>().HasData(states);

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
