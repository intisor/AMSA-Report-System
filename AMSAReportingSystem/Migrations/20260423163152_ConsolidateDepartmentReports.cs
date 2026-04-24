using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AMSAReportingSystem.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateDepartmentReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportingCycles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CycleMonth = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmissionDeadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    ReminderSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportingCycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "States",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Abbreviation = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_States", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reminders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CycleId = table.Column<int>(type: "int", nullable: false),
                    ReminderType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TargetRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TargetState = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ScheduledFor = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reminders_ReportingCycles_CycleId",
                        column: x => x.CycleId,
                        principalTable: "ReportingCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StateReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateId = table.Column<int>(type: "int", nullable: false),
                    CycleId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UnitPresidentsAttended = table.Column<int>(type: "int", nullable: false),
                    TotalUnitPresidents = table.Column<int>(type: "int", nullable: false),
                    UnitPerformanceRating = table.Column<int>(type: "int", nullable: true),
                    UnitImprovementPlan = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ChallengesFaced = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NationalSupportNeeded = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OtherNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PresidentialNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedByPresidentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByPresidentMemberId = table.Column<int>(type: "int", nullable: true),
                    NationalNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AcknowledgedByNationalAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcknowledgedByNationalMemberId = table.Column<int>(type: "int", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedByMemberId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StateReports_ReportingCycles_CycleId",
                        column: x => x.CycleId,
                        principalTable: "ReportingCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StateReports_States_StateId",
                        column: x => x.StateId,
                        principalTable: "States",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StateId = table.Column<int>(type: "int", nullable: false),
                    AmsaDbUnitId = table.Column<int>(type: "int", nullable: false),
                    PresidentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Units_States_StateId",
                        column: x => x.StateId,
                        principalTable: "States",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StateReportActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateReportId = table.Column<int>(type: "int", nullable: false),
                    ActivityTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Objectives = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Outcomes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AttendanceCount = table.Column<int>(type: "int", nullable: true),
                    BeneficiaryCount = table.Column<int>(type: "int", nullable: true),
                    ActivityDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateReportActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StateReportActivities_StateReports_StateReportId",
                        column: x => x.StateReportId,
                        principalTable: "StateReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StateReportActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateReportId = table.Column<int>(type: "int", nullable: false),
                    ActionByMemberId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActionAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateReportActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StateReportActivityLogs_StateReports_StateReportId",
                        column: x => x.StateReportId,
                        principalTable: "StateReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StateReportAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateReportId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedByMemberId = table.Column<int>(type: "int", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateReportAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StateReportAttachments_StateReports_StateReportId",
                        column: x => x.StateReportId,
                        principalTable: "StateReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    CycleId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsCompliant = table.Column<bool>(type: "bit", nullable: false),
                    SubmittedToPresidentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedByMemberId = table.Column<int>(type: "int", nullable: true),
                    ApprovedByPresidentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByPresidentMemberId = table.Column<int>(type: "int", nullable: true),
                    PresidentialNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ApprovedByStateAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByStateMemberId = table.Column<int>(type: "int", nullable: true),
                    StateNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AcknowledgedByNationalAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcknowledgedByNationalMemberId = table.Column<int>(type: "int", nullable: true),
                    NationalNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reports_ReportingCycles_CycleId",
                        column: x => x.CycleId,
                        principalTable: "ReportingCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reports_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceChecks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportId = table.Column<int>(type: "int", nullable: false),
                    Department = table.Column<int>(type: "int", nullable: false),
                    IsCompliant = table.Column<bool>(type: "bit", nullable: false),
                    ComplianceNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CheckedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceChecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplianceChecks_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DepartmentReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportId = table.Column<int>(type: "int", nullable: false),
                    Department = table.Column<int>(type: "int", nullable: false),
                    ReportData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSubmitted = table.Column<bool>(type: "bit", nullable: false),
                    IsCompliant = table.Column<bool>(type: "bit", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedByMemberId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartmentReports_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecipientMemberId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ReportId = table.Column<int>(type: "int", nullable: true),
                    StateReportId = table.Column<int>(type: "int", nullable: true),
                    CycleId = table.Column<int>(type: "int", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_ReportingCycles_CycleId",
                        column: x => x.CycleId,
                        principalTable: "ReportingCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_StateReports_StateReportId",
                        column: x => x.StateReportId,
                        principalTable: "StateReports",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReportActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportId = table.Column<int>(type: "int", nullable: false),
                    ActionByMemberId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActionAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportActivityLogs_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentReportId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedByMemberId = table.Column<int>(type: "int", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportAttachments_DepartmentReports_DepartmentReportId",
                        column: x => x.DepartmentReportId,
                        principalTable: "DepartmentReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ReportingCycles",
                columns: new[] { "Id", "CreatedAt", "CycleMonth", "EndDate", "IsLocked", "ReminderSentAt", "StartDate", "SubmissionDeadline", "UpdatedAt" },
                values: new object[] { 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "January 2025", new DateTime(2025, 1, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), false, null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 2, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "States",
                columns: new[] { "Id", "Abbreviation", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "AB", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Abia", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "AD", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Adamawa", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, "AK", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Akwa Ibom", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, "AN", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Anambra", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, "BC", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bauchi", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, "BY", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bayelsa", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7, "BN", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Benue", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 8, "BO", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Borno", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9, "CR", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cross River", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 10, "DT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Delta", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 11, "EB", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Ebonyi", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 12, "ED", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Edo", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 13, "EK", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Ekiti", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 14, "EN", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Enugu", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 15, "FC", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "FCT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 16, "GM", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Gombe", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 17, "IM", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Imo", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 18, "JG", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Jigawa", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 19, "KD", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Kaduna", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 20, "KN", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Kano", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 21, "KT", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Katsina", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 22, "KB", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Kebbi", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 23, "KG", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Kogi", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 24, "KW", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Kwara", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 25, "LG", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Lagos", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 26, "NS", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nasarawa", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 27, "NG", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Niger", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 28, "OG", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Ogun", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 29, "OD", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Ondo", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 30, "OS", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Osun", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 31, "OY", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Oyo", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 32, "PL", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Plateau", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 33, "RV", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Rivers", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 34, "SK", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sokoto", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 35, "TR", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Taraba", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 36, "YB", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Yobe", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 37, "ZM", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Zamfara", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceChecks_ReportId",
                table: "ComplianceChecks",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentReports_ReportId",
                table: "DepartmentReports",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentReports_ReportId_Department",
                table: "DepartmentReports",
                columns: new[] { "ReportId", "Department" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CycleId",
                table: "Notifications",
                column: "CycleId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientMemberId_IsRead",
                table: "Notifications",
                columns: new[] { "RecipientMemberId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ReportId",
                table: "Notifications",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_StateReportId",
                table: "Notifications",
                column: "StateReportId");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_CycleId",
                table: "Reminders",
                column: "CycleId");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_IsProcessed_ScheduledFor",
                table: "Reminders",
                columns: new[] { "IsProcessed", "ScheduledFor" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportActivityLogs_ReportId",
                table: "ReportActivityLogs",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportAttachments_DepartmentReportId",
                table: "ReportAttachments",
                column: "DepartmentReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportingCycles_CycleMonth",
                table: "ReportingCycles",
                column: "CycleMonth",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_CycleId",
                table: "Reports",
                column: "CycleId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_Status",
                table: "Reports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_UnitId",
                table: "Reports",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_UnitId_CycleId",
                table: "Reports",
                columns: new[] { "UnitId", "CycleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateReportActivities_StateReportId",
                table: "StateReportActivities",
                column: "StateReportId");

            migrationBuilder.CreateIndex(
                name: "IX_StateReportActivityLogs_StateReportId",
                table: "StateReportActivityLogs",
                column: "StateReportId");

            migrationBuilder.CreateIndex(
                name: "IX_StateReportAttachments_StateReportId",
                table: "StateReportAttachments",
                column: "StateReportId");

            migrationBuilder.CreateIndex(
                name: "IX_StateReports_CycleId",
                table: "StateReports",
                column: "CycleId");

            migrationBuilder.CreateIndex(
                name: "IX_StateReports_StateId",
                table: "StateReports",
                column: "StateId");

            migrationBuilder.CreateIndex(
                name: "IX_StateReports_StateId_CycleId",
                table: "StateReports",
                columns: new[] { "StateId", "CycleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateReports_Status",
                table: "StateReports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_States_Name",
                table: "States",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Units_AmsaDbUnitId",
                table: "Units",
                column: "AmsaDbUnitId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Units_StateId",
                table: "Units",
                column: "StateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComplianceChecks");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Reminders");

            migrationBuilder.DropTable(
                name: "ReportActivityLogs");

            migrationBuilder.DropTable(
                name: "ReportAttachments");

            migrationBuilder.DropTable(
                name: "StateReportActivities");

            migrationBuilder.DropTable(
                name: "StateReportActivityLogs");

            migrationBuilder.DropTable(
                name: "StateReportAttachments");

            migrationBuilder.DropTable(
                name: "DepartmentReports");

            migrationBuilder.DropTable(
                name: "StateReports");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "ReportingCycles");

            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropTable(
                name: "States");
        }
    }
}
