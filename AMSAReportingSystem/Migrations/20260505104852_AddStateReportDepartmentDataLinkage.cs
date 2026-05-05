using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMSAReportingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddStateReportDepartmentDataLinkage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StateReportDepartmentData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateReportId = table.Column<int>(type: "int", nullable: false),
                    DepartmentReportId = table.Column<int>(type: "int", nullable: false),
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
                name: "IX_StateReportDepartmentData_DepartmentReportId",
                table: "StateReportDepartmentData",
                column: "DepartmentReportId");

            migrationBuilder.CreateIndex(
                name: "IX_StateReportDepartmentData_StateReportId_DepartmentReportId",
                table: "StateReportDepartmentData",
                columns: new[] { "StateReportId", "DepartmentReportId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StateReportDepartmentData");
        }
    }
}
