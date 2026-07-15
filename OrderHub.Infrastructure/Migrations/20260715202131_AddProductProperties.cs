using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "properties",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "varchar(100)", nullable: false),
                    property_type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_properties", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "order_item_properties",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    order_item_id = table.Column<int>(type: "INTEGER", nullable: false),
                    property_id = table.Column<int>(type: "INTEGER", nullable: false),
                    value = table.Column<string>(type: "varchar(100)", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_item_properties", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_item_properties_order_items_order_item_id",
                        column: x => x.order_item_id,
                        principalTable: "order_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_order_item_properties_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_properties",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    product_id = table.Column<int>(type: "INTEGER", nullable: false),
                    property_id = table.Column<int>(type: "INTEGER", nullable: false),
                    is_required = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_properties", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_properties_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_properties_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "property_options",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    property_id = table.Column<int>(type: "INTEGER", nullable: false),
                    value = table.Column<string>(type: "varchar(100)", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_property_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_property_options_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_order_item_properties_order_item_id_property_id",
                table: "order_item_properties",
                columns: new[] { "order_item_id", "property_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_order_item_properties_property_id",
                table: "order_item_properties",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_properties_product_id_property_id",
                table: "product_properties",
                columns: new[] { "product_id", "property_id" });

            migrationBuilder.CreateIndex(
                name: "ix_product_properties_property_id",
                table: "product_properties",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_properties_name",
                table: "properties",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_property_options_property_id_value",
                table: "property_options",
                columns: new[] { "property_id", "value" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_item_properties");

            migrationBuilder.DropTable(
                name: "product_properties");

            migrationBuilder.DropTable(
                name: "property_options");

            migrationBuilder.DropTable(
                name: "properties");
        }
    }
}
