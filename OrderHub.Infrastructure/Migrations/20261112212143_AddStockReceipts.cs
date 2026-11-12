using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stock_receipts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    warehouse_id = table.Column<int>(type: "integer", nullable: false),
                    receipt_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: true),
                    reference_note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    created_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_receipts", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_receipts_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_receipts_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_receipts_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_receipt_attachments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    stock_receipt_id = table.Column<int>(type: "integer", nullable: true),
                    warehouse_id = table.Column<int>(type: "integer", nullable: false),
                    stored_file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    uploaded_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_receipt_attachments", x => x.id);
                    table.CheckConstraint("ck_stock_receipt_attachments_size", "size_bytes > 0");
                    table.ForeignKey(
                        name: "fk_stock_receipt_attachments_stock_receipts_stock_receipt_id",
                        column: x => x.stock_receipt_id,
                        principalTable: "stock_receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_receipt_attachments_users_uploaded_by_user_id",
                        column: x => x.uploaded_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_receipt_attachments_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_receipt_lines",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    stock_receipt_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_receipt_lines", x => x.id);
                    table.CheckConstraint("ck_stock_receipt_lines_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_stock_receipt_lines_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_receipt_lines_stock_receipts_stock_receipt_id",
                        column: x => x.stock_receipt_id,
                        principalTable: "stock_receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_source_stock_receipt_id",
                table: "inventory_movements",
                column: "source_stock_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_receipt_attachments_stock_receipt_id",
                table: "stock_receipt_attachments",
                column: "stock_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_receipt_attachments_stored_file_name",
                table: "stock_receipt_attachments",
                column: "stored_file_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_receipt_attachments_uploaded_by_user_id",
                table: "stock_receipt_attachments",
                column: "uploaded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_receipt_attachments_warehouse_id_uploaded_by_user_id_",
                table: "stock_receipt_attachments",
                columns: new[] { "warehouse_id", "uploaded_by_user_id", "stock_receipt_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_receipt_lines_product_id",
                table: "stock_receipt_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_receipt_lines_stock_receipt_id_product_id",
                table: "stock_receipt_lines",
                columns: new[] { "stock_receipt_id", "product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_receipts_created_by_user_id",
                table: "stock_receipts",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_receipts_supplier_id",
                table: "stock_receipts",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_receipts_warehouse_id_created_at",
                table: "stock_receipts",
                columns: new[] { "warehouse_id", "created_at" });

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_movements_stock_receipts_source_stock_receipt_id",
                table: "inventory_movements",
                column: "source_stock_receipt_id",
                principalTable: "stock_receipts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_inventory_movements_stock_receipts_source_stock_receipt_id",
                table: "inventory_movements");

            migrationBuilder.DropTable(
                name: "stock_receipt_attachments");

            migrationBuilder.DropTable(
                name: "stock_receipt_lines");

            migrationBuilder.DropTable(
                name: "stock_receipts");

            migrationBuilder.DropIndex(
                name: "ix_inventory_movements_source_stock_receipt_id",
                table: "inventory_movements");
        }
    }
}
