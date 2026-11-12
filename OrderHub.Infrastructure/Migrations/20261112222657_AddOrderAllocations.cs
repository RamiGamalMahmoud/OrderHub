using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "order_inventory_allocations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    order_item_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    source_kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    warehouse_id = table.Column<int>(type: "integer", nullable: true),
                    supplier_id = table.Column<int>(type: "integer", nullable: true),
                    qty = table.Column<int>(type: "integer", nullable: false),
                    issued_qty = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_inventory_allocations", x => x.id);
                    table.CheckConstraint("ck_order_inventory_allocations_quantity", "qty > 0 AND issued_qty >= 0 AND issued_qty <= qty");
                    table.ForeignKey(
                        name: "fk_order_inventory_allocations_order_items_order_item_id",
                        column: x => x.order_item_id,
                        principalTable: "order_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_inventory_allocations_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_inventory_allocations_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_inventory_allocations_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_order_inventory_allocations_order_id_status",
                table: "order_inventory_allocations",
                columns: new[] { "order_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_order_inventory_allocations_order_item_id_source_kind_wareh",
                table: "order_inventory_allocations",
                columns: new[] { "order_item_id", "source_kind", "warehouse_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_order_inventory_allocations_supplier_id",
                table: "order_inventory_allocations",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_inventory_allocations_warehouse_id",
                table: "order_inventory_allocations",
                column: "warehouse_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_inventory_allocations");
        }
    }
}
