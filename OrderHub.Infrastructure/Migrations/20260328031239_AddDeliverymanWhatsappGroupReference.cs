using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliverymanWhatsappGroupReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "whatsapp_group_id",
                table: "deliverymen",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_deliverymen_whatsapp_group_id",
                table: "deliverymen",
                column: "whatsapp_group_id");

            migrationBuilder.AddForeignKey(
                name: "fk_deliverymen_whatsapp_groups_whatsapp_group_id",
                table: "deliverymen",
                column: "whatsapp_group_id",
                principalTable: "whatsapp_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_deliverymen_whatsapp_groups_whatsapp_group_id",
                table: "deliverymen");

            migrationBuilder.DropIndex(
                name: "ix_deliverymen_whatsapp_group_id",
                table: "deliverymen");

            migrationBuilder.DropColumn(
                name: "whatsapp_group_id",
                table: "deliverymen");
        }
    }
}
