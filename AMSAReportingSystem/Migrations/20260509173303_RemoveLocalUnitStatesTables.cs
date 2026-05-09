using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AMSAReportingSystem.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLocalUnitStatesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Units_UnitId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_StateReports_States_StateId",
                table: "StateReports");

            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropTable(
                name: "States");

            migrationBuilder.AddColumn<int>(
                name: "StateId",
                table: "Reports",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_StateId",
                table: "Reports",
                column: "StateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reports_StateId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "StateId",
                table: "Reports");

            migrationBuilder.CreateTable(
                name: "States",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Abbreviation = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_States", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateId = table.Column<int>(type: "int", nullable: false),
                    AmsaDbUnitId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PresidentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
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

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Units_UnitId",
                table: "Reports",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StateReports_States_StateId",
                table: "StateReports",
                column: "StateId",
                principalTable: "States",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
