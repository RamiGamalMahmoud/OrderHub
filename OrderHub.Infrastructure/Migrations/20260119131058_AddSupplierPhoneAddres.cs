using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierPhoneAddres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "address_id",
                table: "suppliers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "phone_id",
                table: "suppliers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_address_id",
                table: "suppliers",
                column: "address_id");

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_phone_id",
                table: "suppliers",
                column: "phone_id");

            migrationBuilder.AddForeignKey(
                name: "fk_suppliers_addresses_address_id",
                table: "suppliers",
                column: "address_id",
                principalTable: "addresses",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_suppliers_phones_phone_id",
                table: "suppliers",
                column: "phone_id",
                principalTable: "phones",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_suppliers_addresses_address_id",
                table: "suppliers");

            migrationBuilder.DropForeignKey(
                name: "fk_suppliers_phones_phone_id",
                table: "suppliers");

            migrationBuilder.DropIndex(
                name: "ix_suppliers_address_id",
                table: "suppliers");

            migrationBuilder.DropIndex(
                name: "ix_suppliers_phone_id",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "address_id",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "phone_id",
                table: "suppliers");
        }
    }
}
