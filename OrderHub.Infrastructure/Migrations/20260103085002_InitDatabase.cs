using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    parent_category_id = table.Column<int>(type: "INTEGER", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_categories_categories_parent_category_id",
                        column: x => x.parent_category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cities",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clients",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "VARCHAR(100)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "phones",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    country_code = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: true),
                    number = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: true),
                    is_primary = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_phones", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    open_at = table.Column<TimeSpan>(type: "time", nullable: true),
                    close_at = table.Column<TimeSpan>(type: "time", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suppliers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "VARCHAR(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "VARCHAR(10)", maxLength: 10, nullable: false),
                    price = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: false),
                    category_id = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                    table.ForeignKey(
                        name: "fk_products_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "addresses",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    street = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    city_id = table.Column<int>(type: "INTEGER", nullable: true),
                    is_primary = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_addresses", x => x.id);
                    table.ForeignKey(
                        name: "fk_addresses_cities_city_id",
                        column: x => x.city_id,
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "deliveryman",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "VARCHAR(100)", maxLength: 100, nullable: false),
                    deliveryman_city_id = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deliveryman", x => x.id);
                    table.ForeignKey(
                        name: "fk_deliveryman_cities_deliveryman_city_id",
                        column: x => x.deliveryman_city_id,
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.CreateTable(
                name: "product_supplier",
                columns: table => new
                {
                    product_id = table.Column<int>(type: "INTEGER", nullable: false),
                    suppliers_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_supplier", x => new { x.product_id, x.suppliers_id });
                    table.ForeignKey(
                        name: "fk_product_supplier_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_product_supplier_suppliers_suppliers_id",
                        column: x => x.suppliers_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "deliveryman_phone",
                columns: table => new
                {
                    deliveryman_id = table.Column<int>(type: "INTEGER", nullable: false),
                    phones_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deliveryman_phone", x => new { x.deliveryman_id, x.phones_id });
                    table.ForeignKey(
                        name: "fk_deliveryman_phone_deliveryman_deliveryman_id",
                        column: x => x.deliveryman_id,
                        principalTable: "deliveryman",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_deliveryman_phone_phones_phones_id",
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
                name: "ix_address_supplier_supplier_id",
                table: "address_supplier",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_addresses_city_id",
                table: "addresses",
                column: "city_id");

            migrationBuilder.CreateIndex(
                name: "ix_categories_parent_category_id",
                table: "categories",
                column: "parent_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_cities_name",
                table: "cities",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_client_phone_phones_id",
                table: "client_phone",
                column: "phones_id");

            migrationBuilder.CreateIndex(
                name: "ix_deliveryman_deliveryman_city_id",
                table: "deliveryman",
                column: "deliveryman_city_id");

            migrationBuilder.CreateIndex(
                name: "ix_deliveryman_phone_phones_id",
                table: "deliveryman_phone",
                column: "phones_id");

            migrationBuilder.CreateIndex(
                name: "ix_phone_supplier_supplier_id",
                table: "phone_supplier",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_supplier_suppliers_id",
                table: "product_supplier",
                column: "suppliers_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_category_id",
                table: "products",
                column: "category_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "address_client");

            migrationBuilder.DropTable(
                name: "address_supplier");

            migrationBuilder.DropTable(
                name: "client_phone");

            migrationBuilder.DropTable(
                name: "deliveryman_phone");

            migrationBuilder.DropTable(
                name: "phone_supplier");

            migrationBuilder.DropTable(
                name: "product_supplier");

            migrationBuilder.DropTable(
                name: "addresses");

            migrationBuilder.DropTable(
                name: "clients");

            migrationBuilder.DropTable(
                name: "deliveryman");

            migrationBuilder.DropTable(
                name: "phones");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "suppliers");

            migrationBuilder.DropTable(
                name: "cities");

            migrationBuilder.DropTable(
                name: "categories");
        }
    }
}
