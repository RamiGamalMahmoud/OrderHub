using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WireInvoiceWithOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "order_id",
                table: "invoices",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "order_number",
                table: "invoices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoices_order_id",
                table: "invoices",
                column: "order_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_invoices_orders_order_id",
                table: "invoices",
                column: "order_id",
                principalTable: "orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_invoices_orders_order_id",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "ix_invoices_order_id",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "order_id",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "order_number",
                table: "invoices");
        }
    }
}
