using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingCarrierToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "shipping_carrier_id",
                table: "orders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_orders_shipping_carrier_id",
                table: "orders",
                column: "shipping_carrier_id");

            migrationBuilder.AddForeignKey(
                name: "fk_orders_shipping_carriers_shipping_carrier_id",
                table: "orders",
                column: "shipping_carrier_id",
                principalTable: "shipping_carriers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_orders_shipping_carriers_shipping_carrier_id",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_orders_shipping_carrier_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_carrier_id",
                table: "orders");
        }
    }
}
