using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryRebuildLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_rebuild_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    performed_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    scope = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    warehouse_id = table.Column<int>(type: "integer", nullable: true),
                    inspected_pairs = table.Column<int>(type: "integer", nullable: false),
                    changed_pairs = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_rebuild_logs", x => x.id);
                    table.CheckConstraint("ck_inventory_rebuild_logs_pairs", "inspected_pairs >= 0 AND changed_pairs >= 0 AND changed_pairs <= inspected_pairs");
                    table.CheckConstraint("ck_inventory_rebuild_logs_scope_warehouse", "(scope = 'SingleWarehouse' AND warehouse_id IS NOT NULL) OR (scope = 'AllWarehouses' AND warehouse_id IS NULL)");
                    table.ForeignKey(
                        name: "fk_inventory_rebuild_logs_users_performed_by_user_id",
                        column: x => x.performed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_rebuild_logs_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_rebuild_log_lines",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rebuild_log_id = table.Column<int>(type: "integer", nullable: false),
                    warehouse_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    before_stock_on_hand = table.Column<int>(type: "integer", nullable: false),
                    before_reserved_on_hand = table.Column<int>(type: "integer", nullable: false),
                    before_damaged_qty = table.Column<int>(type: "integer", nullable: false),
                    after_stock_on_hand = table.Column<int>(type: "integer", nullable: false),
                    after_reserved_on_hand = table.Column<int>(type: "integer", nullable: false),
                    after_damaged_qty = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_rebuild_log_lines", x => x.id);
                    table.CheckConstraint("ck_inventory_rebuild_log_lines_quantities", "before_stock_on_hand >= 0 AND before_reserved_on_hand >= 0 AND before_damaged_qty >= 0 AND after_stock_on_hand >= 0 AND after_reserved_on_hand >= 0 AND after_damaged_qty >= 0");
                    table.ForeignKey(
                        name: "fk_inventory_rebuild_log_lines_inventory_rebuild_logs_rebuild_",
                        column: x => x.rebuild_log_id,
                        principalTable: "inventory_rebuild_logs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_inventory_rebuild_log_lines_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_rebuild_log_lines_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_rebuild_log_lines_product_id",
                table: "inventory_rebuild_log_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_rebuild_log_lines_rebuild_log_id",
                table: "inventory_rebuild_log_lines",
                column: "rebuild_log_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_rebuild_log_lines_warehouse_id_product_id",
                table: "inventory_rebuild_log_lines",
                columns: new[] { "warehouse_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_rebuild_logs_created_at",
                table: "inventory_rebuild_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_rebuild_logs_performed_by_user_id",
                table: "inventory_rebuild_logs",
                column: "performed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_rebuild_logs_warehouse_id",
                table: "inventory_rebuild_logs",
                column: "warehouse_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_rebuild_log_lines");

            migrationBuilder.DropTable(
                name: "inventory_rebuild_logs");
        }
    }
}
