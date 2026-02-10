using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingCarriersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shipping_carriers",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "VARCHAR(100)", maxLength: 100, nullable: false),
                    phone_id = table.Column<int>(type: "INTEGER", nullable: true),
                    address_id = table.Column<int>(type: "INTEGER", nullable: true),
                    price = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shipping_carriers", x => x.id);
                    table.ForeignKey(
                        name: "fk_shipping_carriers_addresses_address_id",
                        column: x => x.address_id,
                        principalTable: "addresses",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_shipping_carriers_phones_phone_id",
                        column: x => x.phone_id,
                        principalTable: "phones",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_shipping_carriers_address_id",
                table: "shipping_carriers",
                column: "address_id");

            migrationBuilder.CreateIndex(
                name: "ix_shipping_carriers_phone_id",
                table: "shipping_carriers",
                column: "phone_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shipping_carriers");
        }
    }
}
