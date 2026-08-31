using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    customer_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    customer_phone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    invoice_number = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    total_amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    total_vat = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "invoices_item",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    product_id = table.Column<int>(type: "INTEGER", nullable: false),
                    invoice_id = table.Column<int>(type: "INTEGER", nullable: false),
                    product_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 3, nullable: false),
                    total = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    vat_rate = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    vat_amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoices_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_invoices_item_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_invoices_item_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_invoices_invoice_number",
                table: "invoices",
                column: "invoice_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoices_item_invoice_id",
                table: "invoices_item",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_item_product_id",
                table: "invoices_item",
                column: "product_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invoices_item");

            migrationBuilder.DropTable(
                name: "invoices");
        }
    }
}
