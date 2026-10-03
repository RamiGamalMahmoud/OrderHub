using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyMessagingInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_deliverymen_whatsapp_groups_whatsapp_group_id",
                table: "deliverymen");

            migrationBuilder.DropForeignKey(
                name: "fk_suppliers_whatsapp_groups_whatsapp_group_id",
                table: "suppliers");

            migrationBuilder.DropTable(
                name: "client_recipients");

            migrationBuilder.DropTable(
                name: "deliveryman_recipients");

            migrationBuilder.DropTable(
                name: "message_attachments");

            migrationBuilder.DropTable(
                name: "order_entity_sequences");

            migrationBuilder.DropTable(
                name: "shippingcarrier_recipients");

            migrationBuilder.DropTable(
                name: "supplier_recipients");

            migrationBuilder.DropTable(
                name: "telegram_accounts");

            migrationBuilder.DropTable(
                name: "telegram_links");

            migrationBuilder.DropTable(
                name: "whatsapp_groups");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "outbox_message_recipients");

            migrationBuilder.DropIndex(
                name: "ix_suppliers_whatsapp_group_id",
                table: "suppliers");

            migrationBuilder.DropIndex(
                name: "ix_deliverymen_whatsapp_group_id",
                table: "deliverymen");

            migrationBuilder.DropColumn(
                name: "whatsapp_group_id",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "whatsapp_group_id",
                table: "deliverymen");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "whatsapp_group_id",
                table: "suppliers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "whatsapp_group_id",
                table: "deliverymen",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "order_entity_sequences",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    display_title = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<int>(type: "integer", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    recipient_type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    sequence_month = table.Column<int>(type: "integer", nullable: false),
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
                    sequence_year = table.Column<int>(type: "integer", nullable: false)
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
                name: "outbox_message_recipients",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    phone_number = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_message_recipients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "telegram_accounts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    chat_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    first_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    last_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    linked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    owner_id = table.Column<int>(type: "integer", nullable: false),
                    owner_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    telegram_user_id = table.Column<long>(type: "bigint", nullable: false),
                    username = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_telegram_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "telegram_links",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    owner_id = table.Column<int>(type: "integer", nullable: false),
                    owner_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_telegram_links", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "whatsapp_groups",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    group_link = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    group_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    group_type = table.Column<string>(type: "text", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_whatsapp_groups", x => x.id);
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
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<int>(type: "integer", nullable: true),
                    recipient_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    max_retries = table.Column<int>(type: "integer", nullable: true, defaultValue: 3),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<List<string>>(type: "text[]", nullable: true),
                    recipient_type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    text = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
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
                name: "message_attachments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    outbox_message_id = table.Column<int>(type: "integer", nullable: false),
                    content_type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    original_file_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    stored_file_name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_whatsapp_group_id",
                table: "suppliers",
                column: "whatsapp_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_deliverymen_whatsapp_group_id",
                table: "deliverymen",
                column: "whatsapp_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_client_recipients_client_id",
                table: "client_recipients",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_deliveryman_recipients_delivery_man_id",
                table: "deliveryman_recipients",
                column: "delivery_man_id");

            migrationBuilder.CreateIndex(
                name: "ix_message_attachments_outbox_message_id",
                table: "message_attachments",
                column: "outbox_message_id");

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
                name: "ix_shippingcarrier_recipients_shipping_carrier_id",
                table: "shippingcarrier_recipients",
                column: "shipping_carrier_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_recipients_supplier_id",
                table: "supplier_recipients",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ux_telegram_accounts_chat_id",
                table: "telegram_accounts",
                column: "chat_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_telegram_accounts_owner",
                table: "telegram_accounts",
                columns: new[] { "owner_type", "owner_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_telegram_accounts_telegram_user_id",
                table: "telegram_accounts",
                column: "telegram_user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_telegram_links_expires_at",
                table: "telegram_links",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_telegram_links_owner",
                table: "telegram_links",
                columns: new[] { "owner_type", "owner_id" });

            migrationBuilder.CreateIndex(
                name: "ux_telegram_links_token",
                table: "telegram_links",
                column: "token",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_deliverymen_whatsapp_groups_whatsapp_group_id",
                table: "deliverymen",
                column: "whatsapp_group_id",
                principalTable: "whatsapp_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_suppliers_whatsapp_groups_whatsapp_group_id",
                table: "suppliers",
                column: "whatsapp_group_id",
                principalTable: "whatsapp_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
