using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_approval_requests_single_target",
                table: "inventory_approval_requests");

            migrationBuilder.AddColumn<int>(
                name: "disposal_quantity",
                table: "inventory_approval_requests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "product_id",
                table: "inventory_approval_requests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "warehouse_id",
                table: "inventory_approval_requests",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "inventory_returns",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    warehouse_id = table.Column<int>(type: "integer", nullable: false),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    stock_issue_order_id = table.Column<int>(type: "integer", nullable: true),
                    recorded_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    effective_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_returns", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventory_returns_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_returns_stock_issue_orders_stock_issue_order_id",
                        column: x => x.stock_issue_order_id,
                        principalTable: "stock_issue_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_returns_users_recorded_by_user_id",
                        column: x => x.recorded_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_returns_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_return_lines",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    return_id = table.Column<int>(type: "integer", nullable: false),
                    allocation_id = table.Column<int>(type: "integer", nullable: false),
                    order_item_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    intact_qty = table.Column<int>(type: "integer", nullable: false),
                    damaged_qty = table.Column<int>(type: "integer", nullable: false),
                    pending_settled_qty = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_return_lines", x => x.id);
                    table.CheckConstraint("ck_inventory_return_lines_quantities", "intact_qty >= 0 AND damaged_qty >= 0 AND intact_qty + damaged_qty > 0 AND pending_settled_qty >= 0 AND pending_settled_qty <= intact_qty + damaged_qty");
                    table.ForeignKey(
                        name: "fk_inventory_return_lines_inventory_returns_return_id",
                        column: x => x.return_id,
                        principalTable: "inventory_returns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_inventory_return_lines_order_inventory_allocations_allocati",
                        column: x => x.allocation_id,
                        principalTable: "order_inventory_allocations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_return_lines_order_items_order_item_id",
                        column: x => x.order_item_id,
                        principalTable: "order_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_return_lines_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_approval_requests_product_id",
                table: "inventory_approval_requests",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_approval_requests_warehouse_id_product_id",
                table: "inventory_approval_requests",
                columns: new[] { "warehouse_id", "product_id" },
                unique: true,
                filter: "\"status\" = 'Pending' AND \"request_type\" = 'DamagedDisposal'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_approval_requests_single_target",
                table: "inventory_approval_requests",
                sql: "(request_type = 'AllocationEdit' AND order_id IS NOT NULL AND stock_issue_order_id IS NULL AND inventory_movement_id IS NULL AND warehouse_id IS NULL AND product_id IS NULL AND disposal_quantity IS NULL) OR (request_type = 'IssueOrderCancel' AND order_id IS NULL AND stock_issue_order_id IS NOT NULL AND inventory_movement_id IS NULL AND warehouse_id IS NULL AND product_id IS NULL AND disposal_quantity IS NULL) OR (request_type = 'MovementReversal' AND order_id IS NULL AND stock_issue_order_id IS NULL AND inventory_movement_id IS NOT NULL AND warehouse_id IS NULL AND product_id IS NULL AND disposal_quantity IS NULL) OR (request_type = 'DamagedDisposal' AND order_id IS NULL AND stock_issue_order_id IS NULL AND inventory_movement_id IS NULL AND warehouse_id IS NOT NULL AND product_id IS NOT NULL AND disposal_quantity > 0)");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_return_lines_allocation_id",
                table: "inventory_return_lines",
                column: "allocation_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_return_lines_order_item_id",
                table: "inventory_return_lines",
                column: "order_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_return_lines_product_id",
                table: "inventory_return_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_return_lines_return_id_allocation_id",
                table: "inventory_return_lines",
                columns: new[] { "return_id", "allocation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inventory_returns_order_id",
                table: "inventory_returns",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_returns_recorded_by_user_id",
                table: "inventory_returns",
                column: "recorded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_returns_stock_issue_order_id",
                table: "inventory_returns",
                column: "stock_issue_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_returns_warehouse_id",
                table: "inventory_returns",
                column: "warehouse_id");

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_approval_requests_products_product_id",
                table: "inventory_approval_requests",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_approval_requests_warehouses_warehouse_id",
                table: "inventory_approval_requests",
                column: "warehouse_id",
                principalTable: "warehouses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_inventory_approval_requests_products_product_id",
                table: "inventory_approval_requests");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_approval_requests_warehouses_warehouse_id",
                table: "inventory_approval_requests");

            migrationBuilder.DropTable(
                name: "inventory_return_lines");

            migrationBuilder.DropTable(
                name: "inventory_returns");

            migrationBuilder.DropIndex(
                name: "ix_inventory_approval_requests_product_id",
                table: "inventory_approval_requests");

            migrationBuilder.DropIndex(
                name: "ix_inventory_approval_requests_warehouse_id_product_id",
                table: "inventory_approval_requests");

            migrationBuilder.DropCheckConstraint(
                name: "ck_inventory_approval_requests_single_target",
                table: "inventory_approval_requests");

            migrationBuilder.DropColumn(
                name: "disposal_quantity",
                table: "inventory_approval_requests");

            migrationBuilder.DropColumn(
                name: "product_id",
                table: "inventory_approval_requests");

            migrationBuilder.DropColumn(
                name: "warehouse_id",
                table: "inventory_approval_requests");

            migrationBuilder.AddCheckConstraint(
                name: "ck_inventory_approval_requests_single_target",
                table: "inventory_approval_requests",
                sql: "(request_type = 'AllocationEdit' AND order_id IS NOT NULL AND stock_issue_order_id IS NULL AND inventory_movement_id IS NULL) OR (request_type = 'IssueOrderCancel' AND order_id IS NULL AND stock_issue_order_id IS NOT NULL AND inventory_movement_id IS NULL) OR (request_type = 'MovementReversal' AND order_id IS NULL AND stock_issue_order_id IS NULL AND inventory_movement_id IS NOT NULL) OR (request_type = 'DamagedDisposal' AND order_id IS NULL AND stock_issue_order_id IS NULL AND inventory_movement_id IS NULL)");
        }
    }
}
