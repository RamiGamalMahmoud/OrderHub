using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RestrictSupplierProductLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_product_supplier_suppliers_suppliers_id",
                table: "product_supplier");

            migrationBuilder.AddForeignKey(
                name: "fk_product_supplier_suppliers_suppliers_id",
                table: "product_supplier",
                column: "suppliers_id",
                principalTable: "suppliers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_product_supplier_suppliers_suppliers_id",
                table: "product_supplier");

            migrationBuilder.AddForeignKey(
                name: "fk_product_supplier_suppliers_suppliers_id",
                table: "product_supplier",
                column: "suppliers_id",
                principalTable: "suppliers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
