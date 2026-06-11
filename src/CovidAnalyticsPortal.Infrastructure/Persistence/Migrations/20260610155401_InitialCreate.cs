using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CovidAnalyticsPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditTrails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Actor = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    EntityName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Parameters = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    TimestampUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditTrails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CovidStatistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    NewCases = table.Column<long>(type: "INTEGER", nullable: false),
                    CumulativeCases = table.Column<long>(type: "INTEGER", nullable: false),
                    ActiveCases = table.Column<long>(type: "INTEGER", nullable: false),
                    Recovered = table.Column<long>(type: "INTEGER", nullable: false),
                    NewDeaths = table.Column<long>(type: "INTEGER", nullable: false),
                    CumulativeDeaths = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CovidStatistics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StateStatistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StateCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    NewCases = table.Column<long>(type: "INTEGER", nullable: false),
                    CumulativeCases = table.Column<long>(type: "INTEGER", nullable: false),
                    ActiveCases = table.Column<long>(type: "INTEGER", nullable: false),
                    Recovered = table.Column<long>(type: "INTEGER", nullable: false),
                    NewDeaths = table.Column<long>(type: "INTEGER", nullable: false),
                    CumulativeDeaths = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateStatistics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrendRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StateCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    Metric = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    StartValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    EndValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ChangeValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ChangePercentage = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Direction = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrendRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrails_Action",
                table: "AuditTrails",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrails_CorrelationId",
                table: "AuditTrails",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrails_TimestampUtc",
                table: "AuditTrails",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CovidStatistics_Date",
                table: "CovidStatistics",
                column: "Date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateStatistics_State_Date",
                table: "StateStatistics",
                columns: new[] { "StateCode", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditTrails");

            migrationBuilder.DropTable(
                name: "CovidStatistics");

            migrationBuilder.DropTable(
                name: "StateStatistics");

            migrationBuilder.DropTable(
                name: "TrendRecords");
        }
    }
}
