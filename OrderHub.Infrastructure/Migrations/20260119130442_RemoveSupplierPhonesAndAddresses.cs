using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSupplierPhonesAndAddresses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "address_supplier");

            migrationBuilder.DropTable(
                name: "phone_supplier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "address_supplier",
                columns: table => new
                {
                    addresses_id = table.Column<int>(type: "INTEGER", nullable: false),
                    supplier_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_address_supplier", x => new { x.addresses_id, x.supplier_id });
                    table.ForeignKey(
                        name: "fk_address_supplier_addresses_addresses_id",
                        column: x => x.addresses_id,
                        principalTable: "addresses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_address_supplier_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "phone_supplier",
                columns: table => new
                {
                    phones_id = table.Column<int>(type: "INTEGER", nullable: false),
                    supplier_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_phone_supplier", x => new { x.phones_id, x.supplier_id });
                    table.ForeignKey(
                        name: "fk_phone_supplier_phones_phones_id",
                        column: x => x.phones_id,
                        principalTable: "phones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_phone_supplier_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_address_supplier_supplier_id",
                table: "address_supplier",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_phone_supplier_supplier_id",
                table: "phone_supplier",
                column: "supplier_id");
        }
    }
}
