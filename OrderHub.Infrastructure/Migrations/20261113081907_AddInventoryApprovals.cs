using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "pending_return_qty",
                table: "order_inventory_allocations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "inventory_approval_requests",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    request_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    order_id = table.Column<int>(type: "integer", nullable: true),
                    stock_issue_order_id = table.Column<int>(type: "integer", nullable: true),
                    inventory_movement_id = table.Column<int>(type: "integer", nullable: true),
                    payload = table.Column<string>(type: "jsonb", nullable: true),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    target_version = table.Column<int>(type: "integer", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    requested_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    decided_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    decided_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    decision_note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_approval_requests", x => x.id);
                    table.CheckConstraint("ck_inventory_approval_requests_decision", "status NOT IN ('Approved', 'Rejected', 'Stale') OR (decided_at_utc IS NOT NULL AND decision_note IS NOT NULL)");
                    table.CheckConstraint("ck_inventory_approval_requests_reason", "length(trim(reason)) > 0");
                    table.CheckConstraint("ck_inventory_approval_requests_single_target", "(request_type = 'AllocationEdit' AND order_id IS NOT NULL AND stock_issue_order_id IS NULL AND inventory_movement_id IS NULL) OR (request_type = 'IssueOrderCancel' AND order_id IS NULL AND stock_issue_order_id IS NOT NULL AND inventory_movement_id IS NULL) OR (request_type = 'MovementReversal' AND order_id IS NULL AND stock_issue_order_id IS NULL AND inventory_movement_id IS NOT NULL) OR (request_type = 'DamagedDisposal' AND order_id IS NULL AND stock_issue_order_id IS NULL AND inventory_movement_id IS NULL)");
                    table.ForeignKey(
                        name: "fk_inventory_approval_requests_inventory_movements_inventory_m",
                        column: x => x.inventory_movement_id,
                        principalTable: "inventory_movements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_approval_requests_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_approval_requests_stock_issue_orders_stock_issue_",
                        column: x => x.stock_issue_order_id,
                        principalTable: "stock_issue_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_approval_requests_users_decided_by_user_id",
                        column: x => x.decided_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_approval_requests_users_requested_by_user_id",
                        column: x => x.requested_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_order_inventory_allocations_pending_return",
                table: "order_inventory_allocations",
                sql: "pending_return_qty >= 0 AND pending_return_qty <= issued_qty");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_approval_requests_decided_by_user_id",
                table: "inventory_approval_requests",
                column: "decided_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_approval_requests_inventory_movement_id",
                table: "inventory_approval_requests",
                column: "inventory_movement_id",
                unique: true,
                filter: "\"status\" = 'Pending' AND \"request_type\" = 'MovementReversal'");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_approval_requests_order_id",
                table: "inventory_approval_requests",
                column: "order_id",
                unique: true,
                filter: "\"status\" = 'Pending' AND \"request_type\" = 'AllocationEdit'");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_approval_requests_requested_by_user_id",
                table: "inventory_approval_requests",
                column: "requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_approval_requests_status_request_type",
                table: "inventory_approval_requests",
                columns: new[] { "status", "request_type" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_approval_requests_stock_issue_order_id",
                table: "inventory_approval_requests",
                column: "stock_issue_order_id",
                unique: true,
                filter: "\"status\" = 'Pending' AND \"request_type\" = 'IssueOrderCancel'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_approval_requests");

            migrationBuilder.DropCheckConstraint(
                name: "ck_order_inventory_allocations_pending_return",
                table: "order_inventory_allocations");

            migrationBuilder.DropColumn(
                name: "pending_return_qty",
                table: "order_inventory_allocations");
        }
    }
}
