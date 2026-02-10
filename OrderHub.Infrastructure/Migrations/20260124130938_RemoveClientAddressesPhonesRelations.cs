using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveClientAddressesPhonesRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "address_client");

            migrationBuilder.DropTable(
                name: "client_phone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "address_client",
                columns: table => new
                {
                    addresses_id = table.Column<int>(type: "INTEGER", nullable: false),
                    client_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_address_client", x => new { x.addresses_id, x.client_id });
                    table.ForeignKey(
                        name: "fk_address_client_addresses_addresses_id",
                        column: x => x.addresses_id,
                        principalTable: "addresses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_address_client_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client_phone",
                columns: table => new
                {
                    client_id = table.Column<int>(type: "INTEGER", nullable: false),
                    phones_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_client_phone", x => new { x.client_id, x.phones_id });
                    table.ForeignKey(
                        name: "fk_client_phone_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_client_phone_phones_phones_id",
                        column: x => x.phones_id,
                        principalTable: "phones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_address_client_client_id",
                table: "address_client",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_client_phone_phones_id",
                table: "client_phone",
                column: "phones_id");
        }
    }
}
