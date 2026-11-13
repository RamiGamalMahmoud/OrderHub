using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockDeliveryRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stock_delivery_records",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    order_item_id = table.Column<int>(type: "integer", nullable: false),
                    allocation_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    trip_id = table.Column<int>(type: "integer", nullable: true),
                    shipment_id = table.Column<int>(type: "integer", nullable: true),
                    note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    recorded_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    effective_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_delivery_records", x => x.id);
                    table.CheckConstraint("ck_stock_delivery_records_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_stock_delivery_records_delivery_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "delivery_trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_delivery_records_order_inventory_allocations_allocati",
                        column: x => x.allocation_id,
                        principalTable: "order_inventory_allocations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_delivery_records_order_items_order_item_id",
                        column: x => x.order_item_id,
                        principalTable: "order_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_delivery_records_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_delivery_records_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_delivery_records_shipments_shipment_id",
                        column: x => x.shipment_id,
                        principalTable: "shipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_delivery_records_users_recorded_by_user_id",
                        column: x => x.recorded_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stock_delivery_records_allocation_id",
                table: "stock_delivery_records",
                column: "allocation_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_delivery_records_order_id",
                table: "stock_delivery_records",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_delivery_records_order_item_id",
                table: "stock_delivery_records",
                column: "order_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_delivery_records_product_id",
                table: "stock_delivery_records",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_delivery_records_recorded_by_user_id",
                table: "stock_delivery_records",
                column: "recorded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_delivery_records_shipment_id",
                table: "stock_delivery_records",
                column: "shipment_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_delivery_records_trip_id",
                table: "stock_delivery_records",
                column: "trip_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_delivery_records");
        }
    }
}
