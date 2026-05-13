using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMSAReportingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddStateAggregationMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UnitPresidentsAttended",
                table: "StateReports",
                newName: "UnitsAttendedTo");

            migrationBuilder.RenameColumn(
                name: "TotalUnitPresidents",
                table: "StateReports",
                newName: "TotalUnitReportsCount");

            migrationBuilder.AddColumn<bool>(
                name: "IsAggregated",
                table: "StateReports",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAggregatedAt",
                table: "StateReports",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastAggregatedByMemberId",
                table: "StateReports",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAutoAggregated",
                table: "StateReportPrograms",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAggregated",
                table: "StateReports");

            migrationBuilder.DropColumn(
                name: "LastAggregatedAt",
                table: "StateReports");

            migrationBuilder.DropColumn(
                name: "LastAggregatedByMemberId",
                table: "StateReports");

            migrationBuilder.DropColumn(
                name: "IsAutoAggregated",
                table: "StateReportPrograms");

            migrationBuilder.RenameColumn(
                name: "UnitsAttendedTo",
                table: "StateReports",
                newName: "UnitPresidentsAttended");

            migrationBuilder.RenameColumn(
                name: "TotalUnitReportsCount",
                table: "StateReports",
                newName: "TotalUnitPresidents");
        }
    }
}
