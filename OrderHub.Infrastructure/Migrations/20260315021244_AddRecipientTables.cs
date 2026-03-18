using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipientTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outbox_message_recipients",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    phone_number = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_message_recipients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "client_recipients",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    client_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_recipients", x => x.id);
                    table.ForeignKey(
                        name: "fk_client_recipients_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_client_recipients_outbox_message_recipients_id",
                        column: x => x.id,
                        principalTable: "outbox_message_recipients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "deliveryman_recipients",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    delivery_man_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deliveryman_recipients", x => x.id);
                    table.ForeignKey(
                        name: "fk_deliveryman_recipients_deliverymen_delivery_man_id",
                        column: x => x.delivery_man_id,
                        principalTable: "deliverymen",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_deliveryman_recipients_outbox_message_recipients_id",
                        column: x => x.id,
                        principalTable: "outbox_message_recipients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shippingcarrier_recipients",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    shipping_carrier_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shippingcarrier_recipients", x => x.id);
                    table.ForeignKey(
                        name: "fk_shippingcarrier_recipients_outbox_message_recipients_id",
                        column: x => x.id,
                        principalTable: "outbox_message_recipients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_shippingcarrier_recipients_shipping_carriers_shipping_carrier_id",
                        column: x => x.shipping_carrier_id,
                        principalTable: "shipping_carriers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_recipients",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    supplier_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_recipients", x => x.id);
                    table.ForeignKey(
                        name: "fk_supplier_recipients_outbox_message_recipients_id",
                        column: x => x.id,
                        principalTable: "outbox_message_recipients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_supplier_recipients_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_client_recipients_client_id",
                table: "client_recipients",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_deliveryman_recipients_delivery_man_id",
                table: "deliveryman_recipients",
                column: "delivery_man_id");

            migrationBuilder.CreateIndex(
                name: "ix_shippingcarrier_recipients_shipping_carrier_id",
                table: "shippingcarrier_recipients",
                column: "shipping_carrier_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_recipients_supplier_id",
                table: "supplier_recipients",
                column: "supplier_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "client_recipients");

            migrationBuilder.DropTable(
                name: "deliveryman_recipients");

            migrationBuilder.DropTable(
                name: "shippingcarrier_recipients");

            migrationBuilder.DropTable(
                name: "supplier_recipients");

            migrationBuilder.DropTable(
                name: "outbox_message_recipients");
        }
    }
}
