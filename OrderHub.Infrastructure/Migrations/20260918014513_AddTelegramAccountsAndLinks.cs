using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramAccountsAndLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "telegram_accounts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    owner_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    owner_id = table.Column<int>(type: "integer", nullable: false),
                    telegram_user_id = table.Column<long>(type: "bigint", nullable: false),
                    chat_id = table.Column<long>(type: "bigint", nullable: false),
                    username = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    first_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    last_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    linked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                    owner_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    owner_id = table.Column<int>(type: "integer", nullable: false),
                    token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_telegram_links", x => x.id);
                });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "telegram_accounts");

            migrationBuilder.DropTable(
                name: "telegram_links");
        }
    }
}
