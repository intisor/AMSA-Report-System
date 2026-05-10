using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMSAReportingSystem.Migrations
{
    /// <inheritdoc />
    public partial class EnforceStateAndNationalAggregationCoreModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComplianceChecks");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Reminders");

            migrationBuilder.DropTable(
                name: "ReportAttachments");

            migrationBuilder.DropTable(
                name: "StateReportActivities");

            migrationBuilder.DropTable(
                name: "StateReportActivityLogs");

            migrationBuilder.DropTable(
                name: "StateReportAttachments");

            migrationBuilder.DropTable(
                name: "StateReportDepartmentData");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComplianceChecks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportId = table.Column<int>(type: "int", nullable: false),
                    CheckedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ComplianceNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Department = table.Column<int>(type: "int", nullable: false),
                    IsCompliant = table.Column<bool>(type: "bit", nullable: false)
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
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CycleId = table.Column<int>(type: "int", nullable: true),
                    ReportId = table.Column<int>(type: "int", nullable: true),
                    StateReportId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RecipientMemberId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false)
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
                name: "Reminders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CycleId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ReminderType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ScheduledFor = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TargetRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TargetState = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true)
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
                name: "ReportAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentReportId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedByMemberId = table.Column<int>(type: "int", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "StateReportActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateReportId = table.Column<int>(type: "int", nullable: false),
                    ActivityDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActivityTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    AttendanceCount = table.Column<int>(type: "int", nullable: true),
                    BeneficiaryCount = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Objectives = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Outcomes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
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
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ActionAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActionByMemberId = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedByMemberId = table.Column<int>(type: "int", nullable: false)
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
                name: "StateReportDepartmentData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentReportId = table.Column<int>(type: "int", nullable: false),
                    StateReportId = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateReportDepartmentData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StateReportDepartmentData_DepartmentReports_DepartmentReportId",
                        column: x => x.DepartmentReportId,
                        principalTable: "DepartmentReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StateReportDepartmentData_StateReports_StateReportId",
                        column: x => x.StateReportId,
                        principalTable: "StateReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceChecks_ReportId",
                table: "ComplianceChecks",
                column: "ReportId");

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
                name: "IX_ReportAttachments_DepartmentReportId",
                table: "ReportAttachments",
                column: "DepartmentReportId");

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
                name: "IX_StateReportDepartmentData_DepartmentReportId",
                table: "StateReportDepartmentData",
                column: "DepartmentReportId");

            migrationBuilder.CreateIndex(
                name: "IX_StateReportDepartmentData_StateReportId_DepartmentReportId",
                table: "StateReportDepartmentData",
                columns: new[] { "StateReportId", "DepartmentReportId" },
                unique: true);
        }
    }
}
