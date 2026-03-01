using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliverymanToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "deliveryman_id",
                table: "orders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_orders_deliveryman_id",
                table: "orders",
                column: "deliveryman_id");

            migrationBuilder.AddForeignKey(
                name: "fk_orders_deliverymen_deliveryman_id",
                table: "orders",
                column: "deliveryman_id",
                principalTable: "deliverymen",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_orders_deliverymen_deliveryman_id",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_orders_deliveryman_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "deliveryman_id",
                table: "orders");
        }
    }
}
