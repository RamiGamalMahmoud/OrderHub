using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientAddressPhone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "address_id",
                table: "clients",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "phone_id",
                table: "clients",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_clients_address_id",
                table: "clients",
                column: "address_id");

            migrationBuilder.CreateIndex(
                name: "ix_clients_phone_id",
                table: "clients",
                column: "phone_id");

            migrationBuilder.AddForeignKey(
                name: "fk_clients_addresses_address_id",
                table: "clients",
                column: "address_id",
                principalTable: "addresses",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_clients_phones_phone_id",
                table: "clients",
                column: "phone_id",
                principalTable: "phones",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_clients_addresses_address_id",
                table: "clients");

            migrationBuilder.DropForeignKey(
                name: "fk_clients_phones_phone_id",
                table: "clients");

            migrationBuilder.DropIndex(
                name: "ix_clients_address_id",
                table: "clients");

            migrationBuilder.DropIndex(
                name: "ix_clients_phone_id",
                table: "clients");

            migrationBuilder.DropColumn(
                name: "address_id",
                table: "clients");

            migrationBuilder.DropColumn(
                name: "phone_id",
                table: "clients");
        }
    }
}
