using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMSAReportingSystem.Migrations
{
    /// <summary>
    /// EF Core migration that adds aggregation metadata to StateReports and an IsAutoAggregated flag to StateReportPrograms.
    /// Generated manually: apply with `dotnet ef database update` after confirming project migrations configuration.
    /// </summary>
    public partial class AddStateAggregationMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                defaultValue: true);
        }

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
        }
    }
}
