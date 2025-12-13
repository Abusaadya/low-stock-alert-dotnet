using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SallaAlertApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.CreateTable(
            //     name: "Merchants",
            //     columns: table => new
            //     {
            //         MerchantId = table.Column<long>(type: "bigint", nullable: false),
            //         AccessToken = table.Column<string>(type: "text", nullable: false),
            //         RefreshToken = table.Column<string>(type: "text", nullable: false),
            //         ExpiresIn = table.Column<int>(type: "integer", nullable: false),
            //         AlertThreshold = table.Column<int>(type: "integer", nullable: false),
            //         AlertEmail = table.Column<string>(type: "text", nullable: true),
            //         NotifyEmail = table.Column<bool>(type: "boolean", nullable: false),
            //         CustomWebhookUrl = table.Column<string>(type: "text", nullable: true),
            //         TelegramChatId = table.Column<string>(type: "text", nullable: true),
            //         NotifyWebhook = table.Column<bool>(type: "boolean", nullable: false),
            //         UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            //         CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_Merchants", x => x.MerchantId);
            //     });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Merchants");
        }
    }
}
