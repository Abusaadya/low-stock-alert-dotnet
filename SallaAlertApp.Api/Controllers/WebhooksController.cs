using Microsoft.EntityFrameworkCore;
using SallaAlertApp.Api.Data;

namespace SallaAlertApp.Api.Migrations;

public class CreateSubscriptionsTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Subscriptions",
            columns: table => new
            {
                Id = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                MerchantId = table.Column<long>(nullable: false),
                Status = table.Column<int>(nullable: false),
                PlanType = table.Column<int>(nullable: false),
                StartDate = table.Column<DateTime>(nullable: false),
                EndDate = table.Column<DateTime>(nullable: true),
                TrialEndsAt = table.Column<DateTime>(nullable: true),
                MaxTelegramAccounts = table.Column<int>(nullable: false),
                MaxAlertsPerMonth = table.Column<int>(nullable: false),
                AlertsSentThisMonth = table.Column<int>(nullable: false),
                LastResetDate = table.Column<DateTime>(nullable: true),
                CreatedAt = table.Column<DateTime>(nullable: false),
                UpdatedAt = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Subscriptions", x => x.Id);
                table.ForeignKey(
                    name: "FK_Subscriptions_Merchants_MerchantId",
                    column: x => x.MerchantId,
                    principalTable: "Merchants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Subscriptions_MerchantId",
            table: "Subscriptions",
            column: "MerchantId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Subscriptions");
    }
}
