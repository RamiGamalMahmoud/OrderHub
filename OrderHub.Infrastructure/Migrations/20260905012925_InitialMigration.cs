using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    parent_category_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "document_sequences",
                columns: table => new
                {
                    document_type = table.Column<int>(type: "integer", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    last_number = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_sequences", x => new { x.document_type, x.year, x.month });
                });

            migrationBuilder.CreateTable(
                name: "outbox_message_recipients",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    phone_number = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_message_recipients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payment_methods",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_methods", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "phones",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    country_code = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: false),
                    number = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_phones", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "properties",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    property_type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_properties", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "whatsapp_groups",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    group_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    group_link = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    group_type = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_whatsapp_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    category_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    street = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    city_id = table.Column<int>(type: "integer", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                name: "property_options",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    property_id = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "deliverymen",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    phone_number = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: true),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    deliveryman_city_id = table.Column<int>(type: "integer", nullable: false),
                    whatsapp_group_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deliverymen", x => x.id);
                    table.ForeignKey(
                        name: "fk_deliverymen_cities_deliveryman_city_id",
                        column: x => x.deliveryman_city_id,
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_deliverymen_whatsapp_groups_whatsapp_group_id",
                        column: x => x.whatsapp_group_id,
                        principalTable: "whatsapp_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_properties",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    property_id = table.Column<int>(type: "integer", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_properties", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_properties_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_product_properties_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "clients",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    location = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    phone_id = table.Column<int>(type: "integer", nullable: false),
                    address_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clients", x => x.id);
                    table.ForeignKey(
                        name: "fk_clients_addresses_address_id",
                        column: x => x.address_id,
                        principalTable: "addresses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_clients_phones_phone_id",
                        column: x => x.phone_id,
                        principalTable: "phones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shipping_carriers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    phone_id = table.Column<int>(type: "integer", nullable: true),
                    address_id = table.Column<int>(type: "integer", nullable: true),
                    shipping_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    phone_id = table.Column<int>(type: "integer", nullable: true),
                    address_id = table.Column<int>(type: "integer", nullable: true),
                    whatsapp_group_id = table.Column<int>(type: "integer", nullable: true),
                    open_at = table.Column<TimeOnly>(type: "time", nullable: true),
                    close_at = table.Column<TimeOnly>(type: "time", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suppliers", x => x.id);
                    table.ForeignKey(
                        name: "fk_suppliers_addresses_address_id",
                        column: x => x.address_id,
                        principalTable: "addresses",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_suppliers_phones_phone_id",
                        column: x => x.phone_id,
                        principalTable: "phones",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_suppliers_whatsapp_groups_whatsapp_group_id",
                        column: x => x.whatsapp_group_id,
                        principalTable: "whatsapp_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "deliveryman_recipients",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    delivery_man_id = table.Column<int>(type: "integer", nullable: false)
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
                name: "client_recipients",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    client_id = table.Column<int>(type: "integer", nullable: false)
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
                name: "orders",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    client_id = table.Column<int>(type: "integer", nullable: false),
                    order_number = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    delivery_method = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true),
                    order_status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, defaultValue: "Pending"),
                    shipping_carrier_id = table.Column<int>(type: "integer", nullable: true),
                    deliveryman_id = table.Column<int>(type: "integer", nullable: true),
                    payment_method_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_orders_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_orders_deliverymen_deliveryman_id",
                        column: x => x.deliveryman_id,
                        principalTable: "deliverymen",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_orders_payment_methods_payment_method_id",
                        column: x => x.payment_method_id,
                        principalTable: "payment_methods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_orders_shipping_carriers_shipping_carrier_id",
                        column: x => x.shipping_carrier_id,
                        principalTable: "shipping_carriers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shippingcarrier_recipients",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    shipping_carrier_id = table.Column<int>(type: "integer", nullable: false)
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
                        name: "fk_shippingcarrier_recipients_shipping_carriers_shipping_carri",
                        column: x => x.shipping_carrier_id,
                        principalTable: "shipping_carriers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_supplier",
                columns: table => new
                {
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    suppliers_id = table.Column<int>(type: "integer", nullable: false)
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
                name: "supplier_recipients",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    document_number = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    issue_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    customer_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    customer_phone = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    customer_address = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_vat = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoices", x => x.id);
                    table.ForeignKey(
                        name: "fk_invoices_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_delivery_steps",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    step_order = table.Column<int>(type: "integer", nullable: false),
                    delivery_method = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    deliveryman_id = table.Column<int>(type: "integer", nullable: true),
                    shipping_carrier_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_delivery_steps", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_delivery_steps_deliverymen_deliveryman_id",
                        column: x => x.deliveryman_id,
                        principalTable: "deliverymen",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_delivery_steps_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_order_delivery_steps_shipping_carriers_shipping_carrier_id",
                        column: x => x.shipping_carrier_id,
                        principalTable: "shipping_carriers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_entity_sequences",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    recipient_type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    entity_id = table.Column<int>(type: "integer", nullable: false),
                    sequence_year = table.Column<int>(type: "integer", nullable: false),
                    sequence_month = table.Column<int>(type: "integer", nullable: false),
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
                    display_title = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_entity_sequences", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_entity_sequences_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    product_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: true),
                    supplier_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_items_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_order_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_items_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    text = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false),
                    order_id = table.Column<int>(type: "integer", nullable: true),
                    recipient_type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    recipient_id = table.Column<int>(type: "integer", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    max_retries = table.Column<int>(type: "integer", nullable: true, defaultValue: 3),
                    last_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<List<string>>(type: "text[]", nullable: true),
                    legacy_attachments = table.Column<List<string>>(type: "text[]", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_outbox_messages_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_outbox_messages_outbox_message_recipient_recipient_id",
                        column: x => x.recipient_id,
                        principalTable: "outbox_message_recipients",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "proforma_invoices",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source_draft_reference = table.Column<Guid>(type: "uuid", nullable: true),
                    order_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    document_number = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    issue_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    customer_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    customer_phone = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    customer_address = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_vat = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proforma_invoices", x => x.id);
                    table.CheckConstraint("ck_proforma_invoice_source_reference_or_order", "source_draft_reference IS NOT NULL OR order_id IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_proforma_invoices_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quotations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    valid_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    source_draft_reference = table.Column<Guid>(type: "uuid", nullable: true),
                    order_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    document_number = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    issue_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    customer_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    customer_phone = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    customer_address = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_vat = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quotations", x => x.id);
                    table.CheckConstraint("ck_quotation_source_reference_or_order", "source_draft_reference IS NOT NULL OR order_id IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_quotations_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invoice_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    invoice_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    product_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    vat_rate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoice_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_invoice_items_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_invoice_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_item_properties",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_item_id = table.Column<int>(type: "integer", nullable: false),
                    property_id = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                name: "message_attachments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    original_file_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    stored_file_name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    relative_path = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    content_type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    outbox_message_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_message_attachments", x => x.id);
                    table.ForeignKey(
                        name: "fk_message_attachments_outbox_messages_outbox_message_id",
                        column: x => x.outbox_message_id,
                        principalTable: "outbox_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "proforma_invoice_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    proforma_invoice_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    product_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    vat_rate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proforma_invoice_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_proforma_invoice_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_proforma_invoice_items_proforma_invoices_proforma_invoice_id",
                        column: x => x.proforma_invoice_id,
                        principalTable: "proforma_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quotation_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    quotation_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    product_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    vat_rate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quotation_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_quotation_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_quotation_items_quotations_quotation_id",
                        column: x => x.quotation_id,
                        principalTable: "quotations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "payment_methods",
                columns: new[] { "id", "code", "created_at", "description", "display_name", "is_active", "modified_at" },
                values: new object[,]
                {
                    { 1, "ON_DELIVERY", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "الدفع نقداً عند الاستلام", "نقداً", true, null },
                    { 2, "BANK_TRANSFER", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "الدفع عبر التحويل البنكي", "تحويل بنكي", true, null },
                    { 3, "SUPPLIER_ACCOUNT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "الدفع على حساب المورد", "على حساب المورد", true, null },
                    { 4, "SALLA", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "الدفع عبر منصة سلة", "سلة", true, null }
                });

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
                name: "ix_client_recipients_client_id",
                table: "client_recipients",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_clients_address_id",
                table: "clients",
                column: "address_id");

            migrationBuilder.CreateIndex(
                name: "ix_clients_phone_id",
                table: "clients",
                column: "phone_id");

            migrationBuilder.CreateIndex(
                name: "ix_deliveryman_recipients_delivery_man_id",
                table: "deliveryman_recipients",
                column: "delivery_man_id");

            migrationBuilder.CreateIndex(
                name: "ix_deliverymen_deliveryman_city_id",
                table: "deliverymen",
                column: "deliveryman_city_id");

            migrationBuilder.CreateIndex(
                name: "ix_deliverymen_whatsapp_group_id",
                table: "deliverymen",
                column: "whatsapp_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_items_invoice_id",
                table: "invoice_items",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_items_product_id",
                table: "invoice_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_document_number",
                table: "invoices",
                column: "document_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoices_order_id",
                table: "invoices",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_message_attachments_outbox_message_id",
                table: "message_attachments",
                column: "outbox_message_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_delivery_steps_deliveryman_id",
                table: "order_delivery_steps",
                column: "deliveryman_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_delivery_steps_order_id_step_order",
                table: "order_delivery_steps",
                columns: new[] { "order_id", "step_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_order_delivery_steps_shipping_carrier_id",
                table: "order_delivery_steps",
                column: "shipping_carrier_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_entity_sequences_order_id_recipient_type_entity_id",
                table: "order_entity_sequences",
                columns: new[] { "order_id", "recipient_type", "entity_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_order_entity_sequences_recipient_type_entity_id_sequence_ye",
                table: "order_entity_sequences",
                columns: new[] { "recipient_type", "entity_id", "sequence_year", "sequence_month", "sequence_number" },
                unique: true);

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
                name: "ix_order_items_order_id",
                table: "order_items",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_items_product_id",
                table: "order_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_items_supplier_id",
                table: "order_items",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_client_id",
                table: "orders",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_deliveryman_id",
                table: "orders",
                column: "deliveryman_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_payment_method_id",
                table: "orders",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_shipping_carrier_id",
                table: "orders",
                column: "shipping_carrier_id");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_order_id_recipient_id",
                table: "outbox_messages",
                columns: new[] { "order_id", "recipient_id" },
                unique: true,
                filter: "status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_recipient_id",
                table: "outbox_messages",
                column: "recipient_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_methods_code",
                table: "payment_methods",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_methods_display_name",
                table: "payment_methods",
                column: "display_name");

            migrationBuilder.CreateIndex(
                name: "ix_payment_methods_is_active",
                table: "payment_methods",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_product_properties_product_id_property_id",
                table: "product_properties",
                columns: new[] { "product_id", "property_id" });

            migrationBuilder.CreateIndex(
                name: "ix_product_properties_property_id",
                table: "product_properties",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_supplier_suppliers_id",
                table: "product_supplier",
                column: "suppliers_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_category_id",
                table: "products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_proforma_invoice_items_product_id",
                table: "proforma_invoice_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_proforma_invoice_items_proforma_invoice_id",
                table: "proforma_invoice_items",
                column: "proforma_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_proforma_invoices_document_number",
                table: "proforma_invoices",
                column: "document_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_proforma_invoices_order_id",
                table: "proforma_invoices",
                column: "order_id");

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

            migrationBuilder.CreateIndex(
                name: "ix_quotation_items_product_id",
                table: "quotation_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_quotation_items_quotation_id",
                table: "quotation_items",
                column: "quotation_id");

            migrationBuilder.CreateIndex(
                name: "ix_quotations_document_number",
                table: "quotations",
                column: "document_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_quotations_order_id",
                table: "quotations",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_shipping_carriers_address_id",
                table: "shipping_carriers",
                column: "address_id");

            migrationBuilder.CreateIndex(
                name: "ix_shipping_carriers_phone_id",
                table: "shipping_carriers",
                column: "phone_id");

            migrationBuilder.CreateIndex(
                name: "ix_shippingcarrier_recipients_shipping_carrier_id",
                table: "shippingcarrier_recipients",
                column: "shipping_carrier_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_recipients_supplier_id",
                table: "supplier_recipients",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_address_id",
                table: "suppliers",
                column: "address_id");

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_phone_id",
                table: "suppliers",
                column: "phone_id");

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_whatsapp_group_id",
                table: "suppliers",
                column: "whatsapp_group_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "client_recipients");

            migrationBuilder.DropTable(
                name: "deliveryman_recipients");

            migrationBuilder.DropTable(
                name: "document_sequences");

            migrationBuilder.DropTable(
                name: "invoice_items");

            migrationBuilder.DropTable(
                name: "message_attachments");

            migrationBuilder.DropTable(
                name: "order_delivery_steps");

            migrationBuilder.DropTable(
                name: "order_entity_sequences");

            migrationBuilder.DropTable(
                name: "order_item_properties");

            migrationBuilder.DropTable(
                name: "product_properties");

            migrationBuilder.DropTable(
                name: "product_supplier");

            migrationBuilder.DropTable(
                name: "proforma_invoice_items");

            migrationBuilder.DropTable(
                name: "property_options");

            migrationBuilder.DropTable(
                name: "quotation_items");

            migrationBuilder.DropTable(
                name: "shippingcarrier_recipients");

            migrationBuilder.DropTable(
                name: "supplier_recipients");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "order_items");

            migrationBuilder.DropTable(
                name: "proforma_invoices");

            migrationBuilder.DropTable(
                name: "properties");

            migrationBuilder.DropTable(
                name: "quotations");

            migrationBuilder.DropTable(
                name: "outbox_message_recipients");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "suppliers");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "clients");

            migrationBuilder.DropTable(
                name: "deliverymen");

            migrationBuilder.DropTable(
                name: "payment_methods");

            migrationBuilder.DropTable(
                name: "shipping_carriers");

            migrationBuilder.DropTable(
                name: "whatsapp_groups");

            migrationBuilder.DropTable(
                name: "addresses");

            migrationBuilder.DropTable(
                name: "phones");

            migrationBuilder.DropTable(
                name: "cities");
        }
    }
}
