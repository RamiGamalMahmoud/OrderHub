using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotationsAndProformaTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "proforma_invoices",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    source_draft_reference = table.Column<Guid>(type: "TEXT", nullable: true),
                    order_id = table.Column<int>(type: "INTEGER", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    document_number = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    issue_date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    customer_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    customer_phone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    customer_address = table.Column<string>(type: "TEXT", nullable: true),
                    subtotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    total_vat = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proforma_invoices", x => x.id);
                    table.CheckConstraint("ck_proforma_invoice_source_reference_or_order", "source_draft_reference IS NOT NULL OR order_id IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_proforma_invoices_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "quotations",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    valid_until = table.Column<DateTime>(type: "TEXT", nullable: false),
                    source_draft_reference = table.Column<Guid>(type: "TEXT", nullable: true),
                    order_id = table.Column<int>(type: "INTEGER", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    document_number = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    issue_date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    customer_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    customer_phone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    customer_address = table.Column<string>(type: "TEXT", nullable: true),
                    subtotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    total_vat = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quotations", x => x.id);
                    table.CheckConstraint("ck_quotation_source_reference_or_order", "source_draft_reference IS NOT NULL OR order_id IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_quotations_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "proforma_invoice_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    proforma_invoice_id = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    product_id = table.Column<int>(type: "INTEGER", nullable: false),
                    product_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 3, nullable: false),
                    unit_price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    vat_rate = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    subtotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    vat_amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proforma_invoice_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_proforma_invoice_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_proforma_invoice_items_proforma_invoices_proforma_invoice_id",
                        column: x => x.proforma_invoice_id,
                        principalTable: "proforma_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quotation_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    quotation_id = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    product_id = table.Column<int>(type: "INTEGER", nullable: false),
                    product_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 3, nullable: false),
                    unit_price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    vat_rate = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    subtotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    vat_amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quotation_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_quotation_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_quotation_items_quotations_quotation_id",
                        column: x => x.quotation_id,
                        principalTable: "quotations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_proforma_invoice_items_product_id",
                table: "proforma_invoice_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_proforma_invoice_items_proforma_invoice_id",
                table: "proforma_invoice_items",
                column: "proforma_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_proforma_invoices_document_number",
                table: "proforma_invoices",
                column: "document_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_proforma_invoices_order_id",
                table: "proforma_invoices",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_proforma_invoices_source_draft_reference",
                table: "proforma_invoices",
                column: "source_draft_reference");

            migrationBuilder.CreateIndex(
                name: "ix_quotation_items_product_id",
                table: "quotation_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_quotation_items_quotation_id",
                table: "quotation_items",
                column: "quotation_id");

            migrationBuilder.CreateIndex(
                name: "ix_quotations_document_number",
                table: "quotations",
                column: "document_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_quotations_order_id",
                table: "quotations",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_quotations_source_draft_reference",
                table: "quotations",
                column: "source_draft_reference");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "proforma_invoice_items");

            migrationBuilder.DropTable(
                name: "quotation_items");

            migrationBuilder.DropTable(
                name: "proforma_invoices");

            migrationBuilder.DropTable(
                name: "quotations");
        }
    }
}
