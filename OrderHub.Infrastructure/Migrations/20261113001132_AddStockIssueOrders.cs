using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockIssueOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_number_sequences_kind",
                table: "number_sequences");

            migrationBuilder.DropCheckConstraint(
                name: "ck_number_sequences_scope",
                table: "number_sequences");

            migrationBuilder.CreateTable(
                name: "stock_issue_orders",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    warehouse_id = table.Column<int>(type: "integer", nullable: false),
                    requested_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    assigned_user_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    cancel_requested_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    cancel_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    cancel_requested_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    numbering_party_id = table.Column<int>(type: "integer", nullable: false),
                    work_record_number = table.Column<long>(type: "bigint", nullable: false),
                    sequence_year = table.Column<int>(type: "integer", nullable: false),
                    sequence_month = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_issue_orders", x => x.id);
                    table.CheckConstraint("ck_stock_issue_orders_cancel_reason", "status <> 'CancelRequested' OR (cancel_reason IS NOT NULL AND length(trim(cancel_reason)) > 0)");
                    table.CheckConstraint("ck_stock_issue_orders_numbering", "numbering_party_id > 0 AND work_record_number > 0 AND sequence_year > 0 AND sequence_month BETWEEN 1 AND 12");
                    table.ForeignKey(
                        name: "fk_stock_issue_orders_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_issue_orders_users_assigned_user_id",
                        column: x => x.assigned_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_issue_orders_users_cancel_requested_by_user_id",
                        column: x => x.cancel_requested_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_issue_orders_users_requested_by_user_id",
                        column: x => x.requested_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_issue_orders_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_issue_lines",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    issue_order_id = table.Column<int>(type: "integer", nullable: false),
                    allocation_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    requested_qty = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_issue_lines", x => x.id);
                    table.CheckConstraint("ck_stock_issue_lines_quantity", "requested_qty > 0");
                    table.ForeignKey(
                        name: "fk_stock_issue_lines_order_inventory_allocations_allocation_id",
                        column: x => x.allocation_id,
                        principalTable: "order_inventory_allocations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_issue_lines_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_issue_lines_stock_issue_orders_issue_order_id",
                        column: x => x.issue_order_id,
                        principalTable: "stock_issue_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_issue_batches",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    issue_order_line_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    recorded_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    effective_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    shortfall_reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_issue_batches", x => x.id);
                    table.CheckConstraint("ck_stock_issue_batches_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_stock_issue_batches_stock_issue_lines_issue_order_line_id",
                        column: x => x.issue_order_line_id,
                        principalTable: "stock_issue_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_stock_issue_batches_users_recorded_by_user_id",
                        column: x => x.recorded_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_number_sequences_kind",
                table: "number_sequences",
                sql: "kind IN ('Order', 'Supplier', 'Deliveryman', 'ShippingCarrier', 'Warehouse')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_number_sequences_scope",
                table: "number_sequences",
                sql: "(\r\n    kind = 'Order'\r\n    AND party_id = 0\r\n    AND year = 0\r\n    AND month = 0\r\n)\r\nOR\r\n(\r\n    kind IN ('Supplier', 'Deliveryman', 'ShippingCarrier', 'Warehouse')\r\n    AND party_id > 0\r\n    AND year > 0\r\n    AND month BETWEEN 1 AND 12\r\n)");

            migrationBuilder.CreateIndex(
                name: "ix_stock_issue_batches_issue_order_line_id_created_at",
                table: "stock_issue_batches",
                columns: new[] { "issue_order_line_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_issue_batches_recorded_by_user_id",
                table: "stock_issue_batches",
                column: "recorded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_issue_lines_allocation_id",
                table: "stock_issue_lines",
                column: "allocation_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_issue_lines_issue_order_id_allocation_id",
                table: "stock_issue_lines",
                columns: new[] { "issue_order_id", "allocation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_issue_lines_product_id",
                table: "stock_issue_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_issue_orders_assigned_user_id",
                table: "stock_issue_orders",
                column: "assigned_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_issue_orders_cancel_requested_by_user_id",
                table: "stock_issue_orders",
                column: "cancel_requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_issue_orders_numbering_party_id_sequence_year_sequenc",
                table: "stock_issue_orders",
                columns: new[] { "numbering_party_id", "sequence_year", "sequence_month", "work_record_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_issue_orders_order_id",
                table: "stock_issue_orders",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_issue_orders_requested_by_user_id",
                table: "stock_issue_orders",
                column: "requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_issue_orders_warehouse_id_status",
                table: "stock_issue_orders",
                columns: new[] { "warehouse_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_issue_batches");

            migrationBuilder.DropTable(
                name: "stock_issue_lines");

            migrationBuilder.DropTable(
                name: "stock_issue_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_number_sequences_kind",
                table: "number_sequences");

            migrationBuilder.DropCheckConstraint(
                name: "ck_number_sequences_scope",
                table: "number_sequences");

            migrationBuilder.AddCheckConstraint(
                name: "ck_number_sequences_kind",
                table: "number_sequences",
                sql: "kind IN ('Order', 'Supplier', 'Deliveryman', 'ShippingCarrier')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_number_sequences_scope",
                table: "number_sequences",
                sql: "(\r\n    kind = 'Order'\r\n    AND party_id = 0\r\n    AND year = 0\r\n    AND month = 0\r\n)\r\nOR\r\n(\r\n    kind IN ('Supplier', 'Deliveryman', 'ShippingCarrier')\r\n    AND party_id > 0\r\n    AND year > 0\r\n    AND month BETWEEN 1 AND 12\r\n)");
        }
    }
}
