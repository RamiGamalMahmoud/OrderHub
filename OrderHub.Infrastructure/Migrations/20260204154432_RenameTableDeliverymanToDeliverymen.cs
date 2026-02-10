using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameTableDeliverymanToDeliverymen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_deliveryman_cities_deliveryman_city_id",
                table: "deliveryman");

            migrationBuilder.DropPrimaryKey(
                name: "pk_deliveryman",
                table: "deliveryman");

            migrationBuilder.RenameTable(
                name: "deliveryman",
                newName: "deliverymen");

            migrationBuilder.RenameIndex(
                name: "ix_deliveryman_deliveryman_city_id",
                table: "deliverymen",
                newName: "ix_deliverymen_deliveryman_city_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_deliverymen",
                table: "deliverymen",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_deliverymen_cities_deliveryman_city_id",
                table: "deliverymen",
                column: "deliveryman_city_id",
                principalTable: "cities",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_deliverymen_cities_deliveryman_city_id",
                table: "deliverymen");

            migrationBuilder.DropPrimaryKey(
                name: "pk_deliverymen",
                table: "deliverymen");

            migrationBuilder.RenameTable(
                name: "deliverymen",
                newName: "deliveryman");

            migrationBuilder.RenameIndex(
                name: "ix_deliverymen_deliveryman_city_id",
                table: "deliveryman",
                newName: "ix_deliveryman_deliveryman_city_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_deliveryman",
                table: "deliveryman",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_deliveryman_cities_deliveryman_city_id",
                table: "deliveryman",
                column: "deliveryman_city_id",
                principalTable: "cities",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
