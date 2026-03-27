using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierWhatappGroupReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "whatsapp_group_id",
                table: "suppliers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_whatsapp_group_id",
                table: "suppliers",
                column: "whatsapp_group_id");

            migrationBuilder.AddForeignKey(
                name: "fk_suppliers_whatsapp_groups_whatsapp_group_id",
                table: "suppliers",
                column: "whatsapp_group_id",
                principalTable: "whatsapp_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_suppliers_whatsapp_groups_whatsapp_group_id",
                table: "suppliers");

            migrationBuilder.DropIndex(
                name: "ix_suppliers_whatsapp_group_id",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "whatsapp_group_id",
                table: "suppliers");
        }
    }
}
