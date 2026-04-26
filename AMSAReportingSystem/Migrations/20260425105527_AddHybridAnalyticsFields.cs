using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMSAReportingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddHybridAnalyticsFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttendanceCount",
                table: "DepartmentReports",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BeneficiaryCount",
                table: "DepartmentReports",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CycleId",
                table: "DepartmentReports",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DuesCollected",
                table: "DepartmentReports",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedDues",
                table: "DepartmentReports",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasOnCampusActivity",
                table: "DepartmentReports",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MemberParticipantCount",
                table: "DepartmentReports",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProgramCount",
                table: "DepartmentReports",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionsOrganized",
                table: "DepartmentReports",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalMemberCount",
                table: "DepartmentReports",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StateReportPrograms",
                columns: table => new
                {
                    ProgramId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateReportId = table.Column<int>(type: "int", nullable: false),
                    ProgramName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Objectives = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Outcomes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TotalAttendance = table.Column<int>(type: "int", nullable: true),
                    TotalBeneficiaries = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateReportPrograms", x => x.ProgramId);
                    table.ForeignKey(
                        name: "FK_StateReportPrograms_StateReports_StateReportId",
                        column: x => x.StateReportId,
                        principalTable: "StateReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentReports_CycleId",
                table: "DepartmentReports",
                column: "CycleId");

            migrationBuilder.CreateIndex(
                name: "IX_StateReportPrograms_StateReportId",
                table: "StateReportPrograms",
                column: "StateReportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StateReportPrograms");

            migrationBuilder.DropIndex(
                name: "IX_DepartmentReports_CycleId",
                table: "DepartmentReports");

            migrationBuilder.DropColumn(
                name: "AttendanceCount",
                table: "DepartmentReports");

            migrationBuilder.DropColumn(
                name: "BeneficiaryCount",
                table: "DepartmentReports");

            migrationBuilder.DropColumn(
                name: "CycleId",
                table: "DepartmentReports");

            migrationBuilder.DropColumn(
                name: "DuesCollected",
                table: "DepartmentReports");

            migrationBuilder.DropColumn(
                name: "ExpectedDues",
                table: "DepartmentReports");

            migrationBuilder.DropColumn(
                name: "HasOnCampusActivity",
                table: "DepartmentReports");

            migrationBuilder.DropColumn(
                name: "MemberParticipantCount",
                table: "DepartmentReports");

            migrationBuilder.DropColumn(
                name: "ProgramCount",
                table: "DepartmentReports");

            migrationBuilder.DropColumn(
                name: "SessionsOrganized",
                table: "DepartmentReports");

            migrationBuilder.DropColumn(
                name: "TotalMemberCount",
                table: "DepartmentReports");
        }
    }
}
