using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_movements",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    warehouse_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    direction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    movement_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    source_stock_receipt_id = table.Column<int>(type: "integer", nullable: true),
                    source_issue_batch_id = table.Column<int>(type: "integer", nullable: true),
                    source_return_id = table.Column<int>(type: "integer", nullable: true),
                    source_count_id = table.Column<int>(type: "integer", nullable: true),
                    source_delivery_record_id = table.Column<int>(type: "integer", nullable: true),
                    reversal_of_movement_id = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    effective_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_movements", x => x.id);
                    table.CheckConstraint("ck_inventory_movements_quantity", "quantity > 0");
                    table.CheckConstraint("ck_inventory_movements_reversal_reference", "(movement_type = 'Reversal' AND reversal_of_movement_id IS NOT NULL) OR (movement_type <> 'Reversal' AND reversal_of_movement_id IS NULL)");
                    table.ForeignKey(
                        name: "fk_inventory_movements_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_movements_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_balances",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    warehouse_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    stock_on_hand = table.Column<int>(type: "integer", nullable: false),
                    reserved_on_hand = table.Column<int>(type: "integer", nullable: false),
                    damaged_qty = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_balances", x => x.id);
                    table.CheckConstraint("ck_stock_balances_damaged_qty", "damaged_qty >= 0");
                    table.CheckConstraint("ck_stock_balances_reserved_le_stock", "reserved_on_hand <= stock_on_hand");
                    table.CheckConstraint("ck_stock_balances_reserved_on_hand", "reserved_on_hand >= 0");
                    table.CheckConstraint("ck_stock_balances_stock_on_hand", "stock_on_hand >= 0");
                    table.ForeignKey(
                        name: "fk_stock_balances_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_created_by_user_id",
                table: "inventory_movements",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_product_id_effective_at_utc",
                table: "inventory_movements",
                columns: new[] { "product_id", "effective_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_reversal_of_movement_id",
                table: "inventory_movements",
                column: "reversal_of_movement_id",
                unique: true,
                filter: "\"reversal_of_movement_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_warehouse_id_product_id_effective_at_utc",
                table: "inventory_movements",
                columns: new[] { "warehouse_id", "product_id", "effective_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_balances_product_id",
                table: "stock_balances",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_balances_warehouse_id_product_id",
                table: "stock_balances",
                columns: new[] { "warehouse_id", "product_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_movements");

            migrationBuilder.DropTable(
                name: "stock_balances");
        }
    }
}
