using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderDeliverySteps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "order_delivery_steps",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    order_id = table.Column<int>(type: "INTEGER", nullable: false),
                    step_order = table.Column<int>(type: "INTEGER", nullable: false),
                    delivery_method = table.Column<string>(type: "TEXT", nullable: false),
                    deliveryman_id = table.Column<int>(type: "INTEGER", nullable: true),
                    shipping_carrier_id = table.Column<int>(type: "INTEGER", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_delivery_steps", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_delivery_steps_deliverymen_deliveryman_id",
                        column: x => x.deliveryman_id,
                        principalTable: "deliverymen",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_delivery_steps_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_order_delivery_steps_shipping_carriers_shipping_carrier_id",
                        column: x => x.shipping_carrier_id,
                        principalTable: "shipping_carriers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_order_delivery_steps_deliveryman_id",
                table: "order_delivery_steps",
                column: "deliveryman_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_delivery_steps_order_id_step_order",
                table: "order_delivery_steps",
                columns: new[] { "order_id", "step_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_order_delivery_steps_shipping_carrier_id",
                table: "order_delivery_steps",
                column: "shipping_carrier_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_delivery_steps");
        }
    }
}
