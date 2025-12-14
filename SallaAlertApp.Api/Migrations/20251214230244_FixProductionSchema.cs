using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SallaAlertApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixProductionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename columns for Merchants table to match [Column] attributes
            migrationBuilder.RenameColumn(name: "MerchantId", table: "Merchants", newName: "merchant_id");
            migrationBuilder.RenameColumn(name: "AccessToken", table: "Merchants", newName: "access_token");
            migrationBuilder.RenameColumn(name: "RefreshToken", table: "Merchants", newName: "refresh_token");
            migrationBuilder.RenameColumn(name: "ExpiresIn", table: "Merchants", newName: "expires_in");
            migrationBuilder.RenameColumn(name: "AlertThreshold", table: "Merchants", newName: "alert_threshold");
            migrationBuilder.RenameColumn(name: "AlertEmail", table: "Merchants", newName: "alert_email");
            migrationBuilder.RenameColumn(name: "NotifyEmail", table: "Merchants", newName: "notify_email");
            migrationBuilder.RenameColumn(name: "CustomWebhookUrl", table: "Merchants", newName: "custom_webhook_url");
            migrationBuilder.RenameColumn(name: "TelegramChatId", table: "Merchants", newName: "telegram_chat_id");
            migrationBuilder.RenameColumn(name: "NotifyWebhook", table: "Merchants", newName: "notify_webhook");
            migrationBuilder.RenameColumn(name: "UpdatedAt", table: "Merchants", newName: "updatedAt");
            migrationBuilder.RenameColumn(name: "CreatedAt", table: "Merchants", newName: "createdAt");

            // Create Subscriptions table
            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MerchantId = table.Column<long>(type: "bigint", nullable: false),
                    PlanType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TrialEndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MaxTelegramAccounts = table.Column<int>(type: "integer", nullable: false),
                    MaxAlertsPerMonth = table.Column<int>(type: "integer", nullable: false),
                    AlertsSentThisMonth = table.Column<int>(type: "integer", nullable: false),
                    LastResetDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "merchant_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_MerchantId",
                table: "Subscriptions",
                column: "MerchantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Subscriptions");

            migrationBuilder.RenameColumn(name: "merchant_id", table: "Merchants", newName: "MerchantId");
            migrationBuilder.RenameColumn(name: "access_token", table: "Merchants", newName: "AccessToken");
            migrationBuilder.RenameColumn(name: "refresh_token", table: "Merchants", newName: "RefreshToken");
            migrationBuilder.RenameColumn(name: "expires_in", table: "Merchants", newName: "ExpiresIn");
            migrationBuilder.RenameColumn(name: "alert_threshold", table: "Merchants", newName: "AlertThreshold");
            migrationBuilder.RenameColumn(name: "alert_email", table: "Merchants", newName: "AlertEmail");
            migrationBuilder.RenameColumn(name: "notify_email", table: "Merchants", newName: "NotifyEmail");
            migrationBuilder.RenameColumn(name: "custom_webhook_url", table: "Merchants", newName: "CustomWebhookUrl");
            migrationBuilder.RenameColumn(name: "telegram_chat_id", table: "Merchants", newName: "TelegramChatId");
            migrationBuilder.RenameColumn(name: "notify_webhook", table: "Merchants", newName: "NotifyWebhook");
            migrationBuilder.RenameColumn(name: "updatedAt", table: "Merchants", newName: "UpdatedAt");
            migrationBuilder.RenameColumn(name: "createdAt", table: "Merchants", newName: "CreatedAt");
        }
    }
}
