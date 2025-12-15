using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SallaAlertApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMerchantName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "Merchants",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "name",
                table: "Merchants");
        }
    }
}
