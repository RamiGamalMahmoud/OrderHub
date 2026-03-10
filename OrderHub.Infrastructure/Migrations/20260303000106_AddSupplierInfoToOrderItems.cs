using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierInfoToOrderItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "supplier_id",
                table: "order_items",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "supplier_name",
                table: "order_items",
                type: "VARCHAR(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_order_items_supplier_id",
                table: "order_items",
                column: "supplier_id");

            migrationBuilder.AddForeignKey(
                name: "fk_order_items_suppliers_supplier_id",
                table: "order_items",
                column: "supplier_id",
                principalTable: "suppliers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_order_items_suppliers_supplier_id",
                table: "order_items");

            migrationBuilder.DropIndex(
                name: "ix_order_items_supplier_id",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "supplier_id",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "supplier_name",
                table: "order_items");
        }
    }
}
