using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "address_id",
                table: "warehouses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "phone_id",
                table: "warehouses",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_warehouses_address_id",
                table: "warehouses",
                column: "address_id");

            migrationBuilder.CreateIndex(
                name: "ix_warehouses_phone_id",
                table: "warehouses",
                column: "phone_id");

            migrationBuilder.AddForeignKey(
                name: "fk_warehouses_addresses_address_id",
                table: "warehouses",
                column: "address_id",
                principalTable: "addresses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_warehouses_phones_phone_id",
                table: "warehouses",
                column: "phone_id",
                principalTable: "phones",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_warehouses_addresses_address_id",
                table: "warehouses");

            migrationBuilder.DropForeignKey(
                name: "fk_warehouses_phones_phone_id",
                table: "warehouses");

            migrationBuilder.DropIndex(
                name: "ix_warehouses_address_id",
                table: "warehouses");

            migrationBuilder.DropIndex(
                name: "ix_warehouses_phone_id",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "address_id",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "phone_id",
                table: "warehouses");
        }
    }
}
