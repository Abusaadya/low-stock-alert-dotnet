using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SallaAlertApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class ResolvePendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastMonthlyReportSentAt",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastWeeklyReportSentAt",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastMonthlyReportSentAt",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "LastWeeklyReportSentAt",
                table: "Subscriptions");
        }
    }
}
