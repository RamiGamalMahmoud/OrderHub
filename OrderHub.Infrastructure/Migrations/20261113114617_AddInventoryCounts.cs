using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryCounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_counts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    warehouse_id = table.Column<int>(type: "integer", nullable: false),
                    scope = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    snapshot_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    started_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    decided_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    decided_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    decision_note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_counts", x => x.id);
                    table.CheckConstraint("ck_inventory_counts_decision", "status NOT IN ('Approved', 'Cancelled') OR (decided_at_utc IS NOT NULL AND decision_note IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_inventory_counts_users_decided_by_user_id",
                        column: x => x.decided_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_counts_users_started_by_user_id",
                        column: x => x.started_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_counts_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_count_lines",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    count_id = table.Column<int>(type: "integer", nullable: false),
                    warehouse_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    book_qty = table.Column<int>(type: "integer", nullable: false),
                    book_damaged_qty = table.Column<int>(type: "integer", nullable: false),
                    counted_qty = table.Column<int>(type: "integer", nullable: true),
                    counted_damaged_qty = table.Column<int>(type: "integer", nullable: true),
                    snapshot_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    snapshot_recorded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reviewed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    review_decision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    review_note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_count_lines", x => x.id);
                    table.CheckConstraint("ck_inventory_count_lines_counted", "(counted_qty IS NULL OR counted_qty >= 0) AND (counted_damaged_qty IS NULL OR counted_damaged_qty >= 0)");
                    table.ForeignKey(
                        name: "fk_inventory_count_lines_inventory_counts_count_id",
                        column: x => x.count_id,
                        principalTable: "inventory_counts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_inventory_count_lines_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_count_lines_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_count_lines_count_id",
                table: "inventory_count_lines",
                column: "count_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_count_lines_product_id",
                table: "inventory_count_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_count_lines_warehouse_id_product_id",
                table: "inventory_count_lines",
                columns: new[] { "warehouse_id", "product_id" },
                unique: true,
                filter: "\"is_active\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_counts_created_at",
                table: "inventory_counts",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_counts_decided_by_user_id",
                table: "inventory_counts",
                column: "decided_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_counts_started_by_user_id",
                table: "inventory_counts",
                column: "started_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_counts_warehouse_id_status",
                table: "inventory_counts",
                columns: new[] { "warehouse_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_count_lines");

            migrationBuilder.DropTable(
                name: "inventory_counts");
        }
    }
}
